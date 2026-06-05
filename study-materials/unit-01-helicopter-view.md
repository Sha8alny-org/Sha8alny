# Unit 1: The Helicopter View — What Is Sha8alny and What Does It Do?

---

## 1.1 The Problem This System Solves

Imagine you are a 3rd-year computer engineering student in Egypt. You know you need real work experience before you graduate — an internship, a freelance project, a graduation project collaboration with an actual company. But how do you find one? You post on LinkedIn, send emails nobody replies to, ask professors, or hope a friend knows someone. It is slow, informal, and unreliable.

Now imagine you are a small tech company in Cairo. You need a part-time developer to build a dashboard, or you want to run a training program for 10 interns this summer. How do you find serious students who can actually deliver? You post on social media, sift through hundreds of unqualified replies, and have no way to verify anything.

**Sha8alny (شغلني — "employ me" in Arabic) solves both problems at once.**

It is a dedicated platform that connects Egyptian university students with companies offering real work opportunities — internships, graduation projects, freelance tasks, and training programs. It handles the entire process in one place: posting opportunities, applying, tracking work progress, processing payments, leaving reviews, and issuing verifiable certificates.

Think of it as a combination of LinkedIn (find the opportunity), Upwork (manage the project and payment), and your university's internship office (official certificates and records) — built specifically for Egyptian university students.

---

## 1.2 The Four People Who Use This System

The system has four types of users. Think of each one as a different person with a different role in the platform.

### The Student
A university student who wants real experience. They create a profile with their skills, CV, education history, and GitHub link. They browse opportunities posted by companies, submit applications with a cover letter and portfolio, track their progress on accepted projects, receive payments for completed work, and collect certificates and reviews for their portfolio.

### The Company
A business — startup, agency, or corporation — that needs student talent. They create a company profile, post project opportunities with detailed requirements and deadlines, review incoming applications, manage accepted students through milestone-based progress tracking, process payments, and leave reviews for students they work with.

### The Admin
The platform operator — one person or a small team who has full visibility over everything. The Admin can see all users, ban bad actors, view platform-wide statistics, manage the skill and university lists, and trigger database backups. Think of Admin as the "God Mode" user who keeps the platform healthy.

### The University *(planned)*
A university representative who can verify that students actually attend the institution they claim. This role exists in the system but the full workflow is not yet implemented.

---

## 1.3 The Journey: From "We Need a Developer" to "Certificate Issued"

Let us follow one complete story from start to finish.

**Ahmad** is a 3rd-year Computer Science student at Cairo University. He has been learning web development for two years and wants his first real freelance experience.

**TechCorp Egypt** is a startup that needs a React developer to build an admin dashboard. They cannot afford a full-time hire, so they are looking for a talented student.

---

**Step 1: TechCorp posts the opportunity.**
A TechCorp manager logs into Sha8alny, creates a company profile, and posts a project: "Admin Dashboard — React Developer Needed." They describe the work, set a 2-week application deadline, tag the required skills (React, JavaScript), and specify the project type as "Part-Time."

**Step 2: Ahmad discovers it.**
Ahmad logs in, browses the project list, and filters by "Part-Time" and "React." He sees TechCorp's project, reads the description, and saves it to his bookmarks.

**Step 3: Ahmad applies.**
Ahmad clicks Apply. He writes a cover letter, links his portfolio, uploads a proposal document, and even suggests a price (bid amount) for the work. His application is submitted and sits in "Pending" status.

**Step 4: TechCorp reviews applications.**
TechCorp's manager opens the applications panel, reads Ahmad's cover letter and portfolio, and decides to accept him. They click "Accept." Ahmad immediately receives a notification: "Congratulations — your application was accepted!"

**Step 5: The work begins — tracked in milestones.**
The TechCorp manager breaks the project into milestones (called "modules"): Milestone 1: "Design the sidebar navigation" — worth 30% of the project. Milestone 2: "Build the data tables" — worth 40%. Milestone 3: "Integrate with the API" — worth 30%.

Ahmad works on Milestone 1. When he finishes, he updates his progress to 100% and marks it done. TechCorp reviews his work and approves it.

**Step 6: Completion.**
After all milestones are approved, TechCorp marks the entire project as complete. The system records Ahmad as having finished a Part-Time project and adds the duration to his "Total Internship Days" counter.

**Step 7: Payment.**
TechCorp processes payment through the platform using Paymob (Egypt's payment gateway). Ahmad gets paid. The payment is recorded with full details.

**Step 8: Mutual reviews.**
TechCorp leaves a detailed review for Ahmad — rating his technical skills, communication, professionalism, and reliability. Ahmad leaves a review for TechCorp — rating their work environment, mentorship quality, and communication. Both reviews are visible on their profiles.

**Step 9: Certificate.**
The system automatically generates a digital certificate for Ahmad: "Ahmad Hassan has completed a Part-Time project with TechCorp Egypt." The certificate has a unique ID. Anyone — including future employers — can verify it by visiting a public URL and entering the ID.

---

## 1.4 What the System Can Do Today (Feature Map)

Here is a plain-English list of everything Sha8alny supports right now, grouped by who uses it.

### What a Student Can Do
- Register an account and verify their email
- Build a complete professional profile (bio, skills, education, experience, CV upload, GitHub link)
- Browse and search project opportunities (filter by type, skills, status)
- Save/bookmark projects they are interested in
- Apply to projects with a cover letter, portfolio, and proposed price
- Track the status of all their applications
- Update progress on project milestones
- Chat with companies inside the platform
- Receive real-time notifications
- Get paid through the platform
- Leave reviews for companies
- Collect and share digital certificates
- Adjust personal settings (notification preferences, language, privacy)

### What a Company Can Do
- Register and create a company profile (logo, description, industry, contact details)
- Post project opportunities with full details (type, skills, deadline, milestones)
- Browse and search student profiles
- Receive and review applications
- Accept or reject applicants
- Create and manage project milestones
- Review and approve student progress on each milestone
- Mark projects as complete
- Process payments to students
- Leave reviews for students
- Chat with students inside the platform

### What an Admin Can Do
- View all users on the platform
- Ban or reactivate users
- View platform-wide statistics (total users, projects, applications, payments)
- Manage the Skills list (add, update, delete skills)
- Trigger database backups on demand

---

## 1.5 The Data That Flows Through the System

What information does each user put into the system, and what do they get back?

**Students put in:**
Name, email, password, university, academic year, department, bio, skills, CV file, education history, work experience, cover letters, portfolio links, milestone progress updates, reviews, chat messages.

**Students get back:**
A professional profile page, accepted/rejected application statuses, milestone feedback from companies, payment receipts, reviews of their work, digital certificates, notifications.

**Companies put in:**
Company name, logo, industry, description, project details (requirements, deadlines, milestones), application decisions, milestone approvals, payment transfers, reviews of students.

**Companies get back:**
A list of matching student applicants, progress updates on active projects, reviews of their company from students, payment confirmations.

**Admins put in:**
Skill names, ban decisions, backup commands.

**Admins get back:**
A dashboard showing platform health: how many users registered, how many projects are active, how many applications were submitted, total payments processed.

---

## 1.6 Why This Is Not Just a Job Board

A job board like Bayt.com or Wuzzuf shows you a listing and then you are on your own — you apply via email, you negotiate outside the platform, you have no record of what happened.

Sha8alny is fundamentally different because it manages the entire relationship inside the platform:

**Milestone tracking** — Work is broken into checkpoints. Progress is visible to both parties at every step. Nobody delivers a black-box result at the end and hopes for the best.

**In-app chat** — All communication is logged inside the platform. No WhatsApp groups, no lost emails. Both parties have a permanent record.

**Payment through the platform** — Money flows through Sha8alny using Egypt's payment system (Paymob). The platform knows when payment was made, how much, and to whom.

**Verifiable certificates** — Certificates have unique IDs. A future employer can verify on the platform that the certificate is real. This is not just a PDF anyone could fake.

**Mutual reviews** — Both parties rate each other. Students build a credibility score over time. Companies with bad reviews get exposed. It is like Uber ratings for professional work.

**Historical record** — Even after a project ends, every student has a permanent history of completed opportunities, total internship days accumulated, and reviews received — their professional portfolio on the platform.

---

## 1.7 What to Say in Your Defense

- "Sha8alny is a full-cycle work platform, not a job board. It manages the entire journey from opportunity discovery to payment and certificate issuance inside one system."
- "We built it specifically for Egyptian university students, which is why it supports Arabic, integrates with Paymob for EGP payments, and includes internship-day tracking relevant to Egyptian graduation requirements."
- "The platform benefits both sides: students get real verified work experience with certificates, and companies get access to a pool of qualified, reviewed students without the overhead of a traditional hiring process."
- "What makes this different from posting on LinkedIn is that everything is tracked, accountable, and verifiable — from milestones to payments to certificates."
- "The system currently supports four user roles: Student, Company, Admin, and University (planned), each with their own set of permissions and capabilities."

---

## 1.8 Self-Check Questions

**Q1: What is the difference between Sha8alny and a regular job board?**
*Hint: Think about what happens after you apply on a job board vs. what Sha8alny manages after acceptance.*

A job board ends at the application. Sha8alny continues through milestone tracking, chat, payment, reviews, and certificate generation — all inside the platform.

**Q2: Who are the four user roles?**
Student, Company, Admin, and University (planned but not yet fully implemented).

**Q3: What is a "milestone" (module) in the context of a project?**
A milestone is a checkpoint in the project — a piece of deliverable work with a defined scope, estimated timeline, and percentage weight. The student reports progress on it; the company reviews and approves it.

**Q4: What is the purpose of a certificate in Sha8alny?**
Certificates give students an official, verifiable record of completed work. They have a unique ID that anyone can verify publicly on the platform.

**Q5: Why does the system track "Total Internship Days" for a student?**
Egyptian universities often require students to accumulate a certain number of internship/training days for graduation. The platform automatically tallies this from all completed projects.

**Q6: What is Paymob, and why does the system use it?**
Paymob is Egypt's leading payment gateway, supporting card payments, mobile wallets, and Fawry kiosk payments. The platform uses it so companies can pay students officially through the system with a tracked record, without handling card numbers directly.

**Q7: Can a student apply to as many projects as they want?**
Yes — a student can apply to multiple projects. However, they cannot apply to the same project twice.
