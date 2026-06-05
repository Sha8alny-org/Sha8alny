# Unit 14: Notifications and SignalR — Real-Time Updates

> **Before reading this unit:** You should have read Unit 3 (request lifecycle — middleware pipeline, how HTTP works) and Unit 6 (JWT authentication — claims). This unit explains how real-time updates work without the user polling every few seconds.

---

## 14.1 The Problem: HTTP Is Request-Response Only

HTTP, the protocol used for all REST API calls, is one-directional by design: the client asks, the server answers. After the answer, the connection closes.

This means: if TechCorp accepts Ahmad's application, the server has no way to tell Ahmad's phone "hey, check this out" — unless Ahmad keeps asking every few seconds ("Did anything happen? How about now? Now?"). This polling approach wastes bandwidth and battery, and is never truly real-time.

**WebSockets** solve this. A WebSocket is a persistent, two-directional connection. Once established, the server can push data to the client at any time. Think of it like a phone call vs. text messaging — HTTP is like sending letters (request-response), WebSocket is like a phone call that stays open.

**SignalR** is Microsoft's library that handles WebSockets (with fallback to older techniques like Long Polling). It abstracts away the connection management so developers write "send to user 42" rather than managing raw socket connections.

---

## 14.2 The Two-Layer Notification Design

Sha8alny uses two parallel channels for every notification:

```
Layer 1: Database persistence
  → INSERT INTO Notifications (UserID, Title, Message, IsRead=false, ...)
  → Always happens. Never fails silently.

Layer 2: Real-time push via SignalR
  → _notifier.SendNotificationAsync(userId, dto)
  → May fail if user is offline. Failure is logged, never thrown.
```

This design is intentional. The database notification serves as the **inbox** — Ahmad can open the app days later and still see "Your application was accepted." The SignalR push serves as the **bell** — if Ahmad is online right now, he hears it immediately.

Even if SignalR fails completely (network issue, user offline), the notification is not lost — it sits in the `Notifications` table. When Ahmad opens the app, `GET /api/Notifications` returns all his notifications including the one he missed.

---

## 14.3 The `Notification` Entity

Every notification is a database row:

```
Notifications table:
NotificationID | UserID | NotificationType | Title | Message
RelatedProjectID | RelatedApplicationID | ActionURL
IsRead | CreatedAt | ReadAt
```

**`NotificationType`** — an enum with 9 values:

| Type | When it is used |
|---|---|
| `Application` | Application status changes (submit, complete) |
| `Acceptance` | Specifically when an application is accepted |
| `Rejection` | Specifically when an application is rejected |
| `Message` | Chat message received |
| `Project` | Project-related events (e.g., all modules complete) |
| `Deadline` | Upcoming deadline reminders |
| `Certificate` | Certificate issued |
| `Payment` | Payment received |
| `System` | General system announcements |

**`ActionURL`** — a path like `/applications/42` that the frontend navigates to when the user taps the notification. This is stored in the database so the notification is actionable even when read later.

**`RelatedProjectID` / `RelatedApplicationID`** — optional FKs that link the notification to specific entities. Allows the frontend to deep-link to the right screen.

---

## 14.4 The `INotifier` Interface — The Abstraction

Inner-layer services (like `ApplicationService`, `ReviewService`) call `INotifier`, not `SignalRNotifier` directly. This is the Onion Architecture rule: services in `Sh8lny.Service` cannot depend on `Microsoft.AspNetCore.SignalR` (which lives in the web layer).

```csharp
public interface INotifier
{
    Task SendNotificationAsync(int userId, NotificationDto notification);
    Task SendNotificationToManyAsync(IEnumerable<int> userIds, NotificationDto notification);
    Task SendMessageToUserAsync(int userId, MessageDto message);
}
```

`SignalRNotifier` (in `Sh8lny.Web`) implements this interface. It is registered in `Program.cs` as the concrete implementation:

```csharp
builder.Services.AddScoped<INotifier, SignalRNotifier>();
```

If the team wanted to switch from SignalR to Firebase Push Notifications tomorrow, they would write a `FirebaseNotifier` class, change one line in `Program.cs`, and all 17 services would automatically use Firebase — none of them need to change.

---

## 14.5 The `NotificationHub` — How WebSocket Connections Work

`NotificationHub` is the SignalR Hub class. It is mapped to the URL `/hubs/notifications` in `Program.cs`:

```csharp
app.MapHub<NotificationHub>("/hubs/notifications");
```

When a client (Flutter app or browser) wants real-time notifications, it opens a WebSocket connection to `wss://api.sha8alny.com/hubs/notifications`. From that point, the connection stays open.

The Hub class is minimal — it only handles connection lifecycle events:

```csharp
[Authorize]     // ← connection requires a valid JWT token
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;  // ← reads UserID from JWT
        _logger.LogInformation("User {UserId} connected", userId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("User {UserId} disconnected", userId);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
```

`Context.UserIdentifier` automatically reads the `ClaimTypes.NameIdentifier` claim from the JWT — the UserID. This is how SignalR knows which user each connection belongs to, without any custom code.

**Groups** — the `JoinGroup` and `LeaveGroup` methods let clients subscribe to named broadcast channels. For example, a client might call `JoinGroup("project-15")` to receive notifications about project 15. This enables future project-specific broadcasts without sending to every connected user.

---

## 14.6 How SignalR Knows Your Identity (JWT via Query String)

This is a tricky problem: WebSocket connections cannot set custom HTTP headers (like `Authorization: Bearer ...`). But our API requires a JWT.

The solution: the JWT token is sent in the **query string** instead. The Program.cs JWT configuration has a special hook:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Normal JWT validation options...
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // For SignalR: read token from query string "?access_token=..."
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

When the Flutter app connects to SignalR, it appends the JWT to the WebSocket URL:
```
wss://api.sha8alny.com/hubs/notifications?access_token=eyJhbGci...
```

The middleware reads this token, validates it exactly like a normal `Authorization: Bearer` header, and populates the user's claims. The Hub then gets `Context.UserIdentifier = "42"` automatically.

---

## 14.7 The `SignalRNotifier` — Pushing Events

`SignalRNotifier` receives an `IHubContext<NotificationHub>` — a server-side handle to the hub that allows pushing to clients without the client having sent a request.

```csharp
// Send to a specific user (all their connected devices):
await _hubContext.Clients.User("42")
    .SendAsync("ReceiveNotification", notificationDto);

// Send to multiple users at once:
await _hubContext.Clients.Users(new[] { "42", "7", "15" })
    .SendAsync("ReceiveNotification", notificationDto);

// Send a chat message:
await _hubContext.Clients.User("42")
    .SendAsync("ReceiveMessage", messageDto);
```

The first argument to `SendAsync` is the **event name** — a string the client listens for:
- `"ReceiveNotification"` — the client registers a handler for this event
- `"ReceiveMessage"` — the client registers a handler for chat messages

The client-side Flutter code would be something like:
```dart
hubConnection.on("ReceiveNotification", (notification) {
    // Show notification toast / update badge count
});
```

**`Clients.User(userId)` vs `Clients.All`:**
- `.User("42")` — sends only to connections where the JWT's NameIdentifier claim is "42". One user on two devices (phone + laptop) both receive it.
- `.All` — sends to every connected user (not used in Sha8alny — never broadcast to everyone).

---

## 14.8 Silent Failure — The Critical Design Decision

Every `catch` block in `SignalRNotifier` looks like this:

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to send real-time notification to user {UserId}", userId);
    // Don't throw - real-time delivery failure shouldn't break the main operation
}
```

The comment explains the intent: if SignalR delivery fails (user offline, network issue, hub error), the exception is logged but NOT rethrown. The calling service's `SendNotificationAsync` call completes silently.

**Why?** Because the database notification was already saved before `INotifier` was called. The real-time push is a bonus — nice-to-have, not critical. If the service threw an exception, the entire operation (accept application, process payment, etc.) would fail, which is unacceptable just because a notification push didn't work.

This is a deliberate reliability trade-off: choose database consistency over real-time delivery. The database notification is always there; the SignalR push is best-effort.

---

## 14.9 The `NotificationService` — Managing the Notification Inbox

`NotificationService` handles CRUD on the `Notifications` table — the "inbox" side:

| Method | What it does |
|---|---|
| `GetUserNotificationsAsync(userId)` | Returns all notifications for the user, newest first |
| `GetUnreadCountAsync(userId)` | Counts notifications where `IsRead == false` (for the badge number) |
| `MarkAsReadAsync(userId, notificationId)` | Sets `IsRead = true` and `ReadAt = now` for one notification |
| `MarkAllAsReadAsync(userId)` | Sets `IsRead = true` for all unread notifications |
| `SendRealTimeNotificationAsync(userId, dto)` | Direct pass-through to `INotifier.SendNotificationAsync` |

**Security:** `MarkAsReadAsync` checks `notification.UserID == userId` before marking — a user cannot mark someone else's notifications as read.

---

## 14.10 What to Say in Your Defense

- "We use ASP.NET Core SignalR for real-time delivery. When a server-side event occurs (application accepted, payment received, etc.), the service saves a `Notification` row to the database AND calls `INotifier.SendNotificationAsync`, which uses SignalR to push the event to the user's active connection."
- "The database notification is the source of truth. SignalR push is best-effort. If the user is offline or the push fails, the notification still exists in the database and they see it when they open the app."
- "We use the `INotifier` interface between inner-layer services and `SignalRNotifier`. This means `ApplicationService` has no knowledge of SignalR — it only knows `INotifier`. If we switch to Firebase tomorrow, only `Program.cs` changes."
- "WebSocket connections can't send HTTP headers, so the JWT is passed as a query string parameter (`?access_token=...`). The JWT middleware is configured with an `OnMessageReceived` event that reads the token from the query string for paths starting with `/hubs`."
- "`SignalRNotifier` uses `IHubContext<NotificationHub>` to push messages from server-side code. `Clients.User(userId.ToString())` targets all WebSocket connections for that specific user — across all their devices."
- "There are two SignalR event types: `ReceiveNotification` (for system notifications — acceptance, payment, etc.) and `ReceiveMessage` (for chat messages). The client subscribes to both and handles them separately in the UI."

---

## 14.11 Self-Check Questions

**Q1: Why is there a database notification AND a SignalR push for the same event?**
Because they serve different purposes. The database notification is persistent — it survives if the user is offline, and they can read it later. The SignalR push is immediate — if the user is online right now, they see it instantly. Both are needed: one for reliability, one for immediacy.

**Q2: What happens if SignalR delivery fails?**
The exception is caught in `SignalRNotifier`, logged as an error, but not rethrown. The calling service continues normally. The database notification is already saved and will be retrieved by the user later.

**Q3: Why does the JWT token travel in the query string for SignalR connections?**
WebSocket connections cannot set custom HTTP headers (including `Authorization`). The JWT Bearer middleware is configured with an `OnMessageReceived` event handler that reads the token from `?access_token=...` in the query string for paths starting with `/hubs`.

**Q4: What is `INotifier` and why is it an interface?**
`INotifier` is the abstraction between inner-layer services and the SignalR infrastructure. It has three methods: `SendNotificationAsync`, `SendNotificationToManyAsync`, and `SendMessageToUserAsync`. Using an interface keeps `Sh8lny.Service` free of any SignalR dependency (Onion Architecture rule). `SignalRNotifier` in `Sh8lny.Web` is the implementation.

**Q5: What is `Context.UserIdentifier` in the Hub?**
It reads the `ClaimTypes.NameIdentifier` claim from the JWT — the user's integer ID as a string. SignalR automatically maps connections to users using this value. When `_hubContext.Clients.User("42")` is called, SignalR delivers to all connections where `UserIdentifier == "42"`.

**Q6: What does `JoinGroup("project-15")` allow?**
It adds the current connection to a SignalR group named "project-15". The server can then call `_hubContext.Clients.Group("project-15").SendAsync(...)` to broadcast to all members of that group. This enables future project-specific notifications without targeting users individually.

**Q7: What is the difference between `ReceiveNotification` and `ReceiveMessage` events?**
Both are SignalR event names. `ReceiveNotification` delivers a `NotificationDto` (system events: acceptance, payment, completion, etc.). `ReceiveMessage` delivers a `MessageDto` (chat messages). The client registers separate handlers for each so the UI can respond differently — a chat message goes to the chat screen, a notification goes to the notification bell.
