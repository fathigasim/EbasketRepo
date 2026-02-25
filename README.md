# e-Shop – Full-Stack E-Commerce Management System

A full-stack e-commerce management system built with **ASP.NET Core**, **React**, and **SQL Server**, designed with clean architecture principles and modern authentication & authorization mechanisms.

This project demonstrates how to build a secure, scalable, and production-ready backend while integrating seamlessly with a modern frontend.

---

## 🚀 Project Overview

**e-Shop** is a full-stack application for managing an online store.  
It focuses on backend best practices such as authentication, authorization, clean architecture, and API security, while providing a React-based frontend for real-world usage.

The system supports advanced authentication, role-based access control, and modular design suitable for real production environments.

---

## 🧱 Tech Stack

### Backend
- ASP.NET Core (.NET Core)
- ASP.NET Core Identity
- Entity Framework Core
- SQL Server
- JWT Authentication + Refresh Tokens
- Clean Architecture
- Swagger

### Frontend
- React
- TypeScript
- REST API Integration

---

## 🏗 Architecture

The backend follows **Clean Architecture**, ensuring:
- Clear separation of concerns
- High testability
- Scalability and maintainability

### Key Patterns & Concepts
- Clean Code
- CQRS with MediatR
- Repository Pattern
- Centralized Exception Handling

---

## 🔐 Authentication & Authorization

- JWT Authentication
- Refresh Token implementation
- ASP.NET Core Identity
- Role-based Authorization (Admin / User, etc.)
- Secure password hashing and validation
- Protected endpoints based on roles and policies

---

## ✨ Key Features

- User registration and login
- JWT + Refresh Token authentication
- Role-based access control
- Secure RESTful APIs
- Product and category management
- Pagination and filtering
- Global exception handling
- Swagger API documentation
- SQL Server database integration
- React frontend consuming the API

---

## ⚙️ Setup & Run

### Backend
1. Update the **connection string** in `appsettings.json`
2. Apply database migrations:
   ```bash
   dotnet ef database update
