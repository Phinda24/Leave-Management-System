# 📋 Leave Management System

A full-stack Leave Management System built using **ASP.NET Core MVC** for the web application and **Flutter** for the mobile application, with **Firebase (Firestore)** as the shared database.

---

## 🚀 Overview

This system allows employees to apply for leave while supervisors and HODs can review, approve, or decline requests based on company policies.

It is designed to simulate a real-world HR workflow and demonstrate enterprise-level application architecture.

---

## 🧩 Features

### 👨‍💼 Employee (Mobile App - Flutter)

* Apply for leave
* View leave status (Pending / Approved / Declined)
* Track leave history

### 🧑‍💼 Supervisor / HOD (Web App - ASP.NET MVC)

* View leave applications
* Approve or decline requests
* Manage employee leave records

---

## 🏗️ Tech Stack

### Web Application

* ASP.NET Core MVC (.NET 8)
* HTML, CSS, JavaScript

### Mobile Application

* Flutter & Dart

### Backend / Database

* Firebase Authentication
* Firebase Firestore (NoSQL Database)

---

## 🔗 System Architecture

* One shared **Firestore database**
* Web app used by management (HOD & Supervisor)
* Mobile app used by employees
* Real-time data synchronization between platforms

---

## ⚙️ Installation & Setup

### 1. Clone the Repository

```bash
git clone https://github.com/Phinda24/Leave-Management-System.git
cd LeaveManagementSystem
```

### 2. Web Application (ASP.NET)

* Open in Visual Studio
* Restore NuGet packages
* Run the project

### 3. Mobile Application (Flutter)

```bash
flutter pub get
flutter run
```

---

## 🔐 Firebase Configuration

* Create a Firebase project
* Enable:

  * Authentication
  * Firestore Database
* Add your Firebase configuration to both:

  * ASP.NET project
  * Flutter app

---





---

## 📸 Screenshots
### Login
### Employee/Dashboard/Apply/balance/history
<img width="1366" height="768" alt="Screenshot 2026-03-28 184410" src="https://github.com/user-attachments/assets/81b362ae-6eba-4025-b637-127a39170cc7" />
<img width="1366" height="768" alt="Screenshot 2026-03-28 184427" src="https://github.com/user-attachments/assets/be395d11-135b-4e2f-9818-4fd3cbfa75ce" />
<img width="1366" height="768" alt="Screenshot 2026-03-28 184446" src="https://github.com/user-attachments/assets/66f2f9fb-2e74-4638-a798-d2d65bc255b0" />
<img width="1366" height="768" alt="Screenshot 2026-03-28 184522" src="https://github.com/user-attachments/assets/5fb0de2f-8ad0-49ca-b61d-e0fc874a997a" />
### HOD/Dashboard/REQUEST/REPORTS
<img width="1366" height="768" alt="Screenshot 2026-03-28 181809" src="https://github.com/user-attachments/assets/53449f48-199a-4edf-9806-78924016e09d" />
<img width="1366" height="768" alt="Screenshot 2026-03-28 181826" src="https://github.com/user-attachments/assets/1c4c575b-fd84-418e-87fc-5cab65119fa4" />
<img width="1366" height="768" alt="Screenshot 2026-03-28 181930" src="https://github.com/user-attachments/assets/d98e3bbd-7e64-4b7e-847a-6d1d52500a45" />
<img width="1366" height="768" alt="Screenshot 2026-03-28 181946" src="https://github.com/user-attachments/assets/b41478f1-ed7a-45fe-84d9-586919285041" />

### SUPERVISOR/Dashboard/REQUEST/REPORTS
<img width="1366" height="768" alt="Screenshot 2026-03-28 182050" src="https://github.com/user-attachments/assets/ce458605-29b0-4f84-9569-c3c20fb4820b" />
<img width="1366" height="768" alt="Screenshot 2026-03-28 182103" src="https://github.com/user-attachments/assets/fa9d69d7-9de8-4d29-aad9-cd0c7a65b67f" />
<img width="1366" height="768" alt="Screenshot 2026-03-28 182138" src="https://github.com/user-attachments/assets/ef54ac93-5a68-414f-a874-7c0249ae0644" />
<img width="1366" height="768" alt="Screenshot 2026-03-28 182226" src="https://github.com/user-attachments/assets/41d4cda4-a711-4f6c-96b1-2ad82dbff379" />
<img width="1366" height="768" alt="Screenshot 2026-03-28 182350" src="https://github.com/user-attachments/assets/17f12511-e93b-4dda-9bf6-0686b8c7207e" />
















---

## 💡 Future Improvements

* Email notifications for leave approval/decline
* Role-based authentication and authorization
* Admin dashboard with analytics
* Leave balance tracking
* Audit logs

---

## 🧠 Learning Outcomes

* Full-stack development with ASP.NET Core
* Mobile development with Flutter
* Cloud database integration using Firebase
* Real-world workflow implementation (HR systems)

---

## 👤 Author

**Phinda Mpho Moloko and Shaun Thando Vulandi**
Junior .NET Developer

---

## 📄 License

This project is for educational and portfolio purposes.
