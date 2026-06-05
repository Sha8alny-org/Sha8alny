# Unit 13: Chat and Messaging — Direct Communication Between Users

> **Before reading this unit:** You should have read Unit 3 (request lifecycle — how REST endpoints work) and Unit 14 (SignalR — how real-time delivery works). If you have not read Unit 14 yet, come back after reading it to understand the real-time delivery step in section 13.6.

---

## 13.1 What the Chat System Does

Sha8alny's chat feature allows students and companies to communicate directly: Ahmad can ask TechCorp questions about a project before applying, or TechCorp can send Ahmad feedback during the internship.

The design decision made here is important: **chat is implemented as a REST API, not a persistent WebSocket connection.**

This means:
- Sending a message = `POST /api/Chat/send`
- Reading messages = `GET /api/Chat/conversations/{id}/messages`

Messages are stored in the database first, then delivered in real-time via SignalR. If the recipient is offline, the message stays in the database and they read it later via the REST endpoint.

Think of it like email (REST) with push notification (SignalR) — not like a live voice call (persistent WebSocket).

---

## 13.2 The Data Model — Three Tables

**`Conversation`** — represents a chat thread:

```
Conversations table:
ConversationID | ConversationType | GroupID | ConversationName
CreatedAt | LastMessageAt
```

- `ConversationType` — either `Direct` (two people) or `Group` (multiple people). Currently only `Direct` conversations are fully implemented.
- `LastMessageAt` — updated every time a new message is sent, used to sort the conversation list by most recent activity.

**`ConversationParticipant`** — the join table that says "this user is in this conversation":

```
ConversationParticipants table:
ConversationID | UserID | JoinedAt | LastReadAt
```

- `LastReadAt` — updated when the user reads the conversation. Used to determine unread count.
- For a Direct conversation, there are always exactly two rows in this table.

**`Message`** — the individual messages:

```
Messages table:
MessageID | ConversationID | SenderID | MessageText | MessageType
IsRead | IsEdited | SentAt
```

- `MessageType` — enum, currently only `Text` is used
- `IsRead` — per-message read flag (separate from `LastReadAt` on the participant)
- `IsEdited` — a flag for future message editing (not yet implemented as an edit endpoint)

---

## 13.3 Find or Create Conversation — The Smart Pattern

When Ahmad sends his first message to TechCorp, there is no conversation between them yet. But when he sends the second message, there should be. The service handles both cases transparently with `FindOrCreateConversationAsync`:

```
Step 1: Load all conversations Ahmad is in
Step 2: For each conversation, check if it is type Direct
Step 3: For each direct conversation, check if TechCorp's UserID is also a participant
Step 4: If found → return the existing conversation (no duplicate)
Step 5: If not found → create a new Conversation row
         + create two ConversationParticipant rows (one per user)
         → return the new conversation
```

This means the client never needs to call a "create conversation" endpoint separately. Sending a message handles everything.

**Why this matters for your defense:** This is the same "find-or-create" (upsert) pattern used in Company profile creation (Unit 8). It simplifies the client — it always calls the same `POST /api/Chat/send` endpoint regardless of whether this is the first message or the hundredth.

---

## 13.4 Sending a Message — The Full Flow

When Ahmad calls `POST /api/Chat/send`:

```
SendMessageDto: { receiverId: 7, content: "Hello, I have a question about the project" }

1. Verify sender (Ahmad's UserID) exists
2. Verify receiver (TechCorp's UserID = 7) exists
3. Prevent self-message: senderId != dto.ReceiverId
4. FindOrCreateConversationAsync(Ahmad.UserID, TechCorp.UserID)
   → returns existing or newly created Conversation
5. Create Message:
   ConversationID = conversation.ConversationID
   SenderID = Ahmad.UserID
   MessageText = "Hello, I have a question about the project"
   MessageType = Text
   IsRead = false
   SentAt = DateTime.UtcNow
6. Update conversation.LastMessageAt = message.SentAt
7. SaveAsync()
8. Resolve Ahmad's display name:
   → check Students table first → "Ahmad Hassan"
   → if not student, check Companies → use CompanyName
   → if not company, fall back to User.Email
9. Build MessageDto response
10. Send real-time via _notifier.SendMessageToUserAsync(TechCorp.UserID, messageDto)
    → SignalR pushes to TechCorp's active connection instantly
11. Return MessageDto (the saved message details)
```

The API response is the saved message itself — including the `MessageID` assigned by the database. This allows the sender to track their own messages.

---

## 13.5 Listing Conversations — The Inbox

`GET /api/Chat/conversations` returns the current user's conversation list — like the inbox screen on WhatsApp.

For each conversation the user participates in:
- Filter out non-Direct conversations (Group is not displayed yet)
- Find the other participant (the user they are talking to)
- Load all messages for the conversation, find the most recent (`lastMessage`)
- Count unread messages: messages where `SenderID != userId && IsRead == false`
- Resolve the other user's display name and profile picture

The result is ordered by `LastMessageAt` descending — most recently active conversations first.

**Display name resolution:** The service checks student and company tables in order:
```csharp
var student = await _unitOfWork.Students.FindSingleAsync(s => s.UserID == userId);
if (student is not null) return student.FullName;

var company = await _unitOfWork.Companies.FindSingleAsync(c => c.UserID == userId);
if (company is not null) return company.CompanyName;

return user?.Email ?? "Unknown User";  // fallback
```

This means the chat uses meaningful names ("Ahmad Hassan", "TechCorp") rather than generic usernames.

---

## 13.6 Real-Time Delivery — How Messages Arrive Instantly

When Ahmad sends a message, TechCorp gets it instantly without polling. This is the `_notifier.SendMessageToUserAsync(receiverId, messageDto)` call at step 10.

`INotifier` has two methods:
- `SendNotificationAsync(userId, notificationDto)` — sends a notification event (used in Units 9–12)
- `SendMessageToUserAsync(userId, messageDto)` — sends a chat message event

Both are implemented by `SignalRNotifier` (covered in depth in Unit 14). The key: SignalR maintains a persistent WebSocket connection between the server and each logged-in user. When the server calls `SendMessageToUserAsync`, it pushes the `MessageDto` to TechCorp's connected device over that WebSocket — immediately, without TechCorp polling for new messages.

If TechCorp is offline (no SignalR connection), the message is NOT lost — it is still saved in the `Messages` table. When TechCorp opens the app later, they call `GET /api/Chat/conversations/{id}/messages` and retrieve all messages including this one.

---

## 13.7 Reading Messages and Mark-as-Read

`GET /api/Chat/conversations/{conversationId}/messages` returns all messages in a conversation, ordered by `SentAt` ascending (oldest first — like a real chat thread).

**Security check:** Before returning messages, the service verifies the requesting user is actually a participant in this conversation. A student cannot read someone else's private conversation.

`PUT /api/Chat/conversations/{conversationId}/read` marks all unread messages as read:
- Finds all messages where `SenderID != userId && IsRead == false`
- Sets `IsRead = true` on each
- Updates `participation.LastReadAt = DateTime.UtcNow`
- SaveAsync()

The unread count shown in `GetMyConversationsAsync` drops to 0 after this call.

---

## 13.8 What Is Not Implemented Yet

The data model supports more than what is currently built:

- **Group conversations:** The `ConversationType.Group` value exists, `GroupID` on `Conversation` links to a `ProjectGroup` entity, but `GetMyConversationsAsync` explicitly skips non-Direct conversations. Group chat is architecturally ready but not yet active.
- **Message editing:** The `IsEdited` flag exists on `Message`, but there is no `EditMessageAsync` method in `ChatService`.
- **File attachments:** The `MessageType` enum only has `Text` used. Image/file message types could be added by accepting a URL (from `/api/Media`) as the message content.
- **Typing indicators:** No "Ahmad is typing..." feature (this would require a SignalR-only event, no database row).

---

## 13.9 What to Say in Your Defense

- "Our chat system uses a REST API for sending and reading messages — not a persistent WebSocket. But messages are delivered in real-time via SignalR: when a message is sent, `INotifier.SendMessageToUserAsync` pushes it to the recipient's SignalR connection instantly."
- "If the recipient is offline, the message is stored in the database and they read it later through the REST endpoint. No messages are lost — REST provides durability, SignalR provides immediacy."
- "Conversations are created automatically on the first message — there is no separate 'create conversation' endpoint. `FindOrCreateConversationAsync` checks for an existing direct conversation between the two users before creating a new one, preventing duplicate conversations."
- "Chat messages are secured: before returning messages for a conversation, the service verifies the requesting user is a `ConversationParticipant` in that conversation. A user cannot read another user's private messages."
- "Display names are resolved dynamically: the service checks the `Students` table first, then `Companies`, then falls back to the `User.Email`. This is why chat shows 'Ahmad Hassan' and 'TechCorp' rather than raw user IDs or emails."

---

## 13.10 Self-Check Questions

**Q1: Is chat implemented using WebSockets or REST?**
REST — messages are sent via `POST /api/Chat/send` and read via `GET /api/Chat/conversations/{id}/messages`. Real-time delivery is layered on top via SignalR's `INotifier.SendMessageToUserAsync`. If the recipient is offline, messages are stored in the database and retrieved later.

**Q2: What is `FindOrCreateConversationAsync` and why does it exist?**
A method that looks for an existing Direct conversation between two users before creating a new one. It prevents duplicate conversations — the first message creates the conversation, subsequent messages reuse it. The client never needs a separate "create conversation" endpoint.

**Q3: How does the service get the display name "Ahmad Hassan" instead of a user ID?**
It checks the `Students` table first (`FindSingleAsync(s => s.UserID == userId)`). If the user is a student, it returns `student.FullName`. If not, it checks `Companies` for `company.CompanyName`. If neither, it falls back to `User.Email`.

**Q4: What does `PUT /api/Chat/conversations/{id}/read` do exactly?**
It finds all messages in the conversation where `SenderID != userId` and `IsRead == false`, sets `IsRead = true` on each, updates `ConversationParticipant.LastReadAt = DateTime.UtcNow`, and saves. This reduces the unread count shown in the conversation list to 0.

**Q5: What security check prevents one user from reading another's private messages?**
`GetConversationMessagesAsync` calls `FindSingleAsync(p => p.ConversationID == conversationId && p.UserID == userId)` to verify the requesting user is a participant. If no matching `ConversationParticipant` row exists, the request is rejected with "You are not a participant in this conversation."

**Q6: Why does the `Conversation` entity have a `GroupID` field if group chat is not implemented?**
The data model was designed with future group chat in mind. `GroupID` links to a `ProjectGroup` entity (a group of students working together on a project). The `ConversationType.Group` value exists and the relationship is mapped, but `GetMyConversationsAsync` currently filters out group conversations. The architecture is ready; only the service logic and endpoints need to be written.

**Q7: What two things does `SendMessageAsync` do after saving the message?**
It calls `_notifier.SendMessageToUserAsync(receiverId, messageDto)` to push the message to the receiver's SignalR connection in real-time. It also updates `conversation.LastMessageAt` to the message's sent time, keeping the inbox sorted correctly.
