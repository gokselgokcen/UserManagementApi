# 🧑‍💻 User Management API (Minimal API with Middleware)

This is a simple RESTful API built with **ASP.NET Core Minimal API**. It provides basic CRUD operations for user management, includes middleware for **token-based authentication**, **request/response logging**, and **global error handling**.

## 🚀 Features

- List users (with pagination)
- Get a user by ID
- Add new users
- Update existing users
- Delete users
- Token-based authentication middleware
- Request and response logging middleware
- Global error handling middleware
- In-memory data storage (no database)

## 🔐 Authentication

All endpoints require a **Bearer token**.

Authorization: Bearer my_secure_key_123
