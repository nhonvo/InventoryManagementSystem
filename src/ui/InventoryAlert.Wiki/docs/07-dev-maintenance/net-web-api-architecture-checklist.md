---
title: .NET Web API Architecture & Quality Checklist
sidebar_position: 8
description: Architectural standards, EF Core transaction rules, C# 12 conventions, and testing guidelines.
---

# ✅ .NET Web API Architecture & Quality Checklist

This checklist defines the architectural requirements, code quality rules, and engineering standards for the InventoryAlert codebase.

---

## 🏛️ 1. Architecture & Layer Discipline

- [x] **Layer Boundaries**: `InventoryAlert.Domain` has **zero imports** from Application, Infrastructure, or Web layers.
- [x] **Primary Constructors**: C# 12 primary constructors are used for dependency injection across services and repositories.
- [x] **No Async Without Await**: Methods returning `Task` without async operations return `Task.FromResult(...)` directly (no `CS1998` warnings).
- [x] **Cancellation Tokens**: `CancellationToken ct` is the last parameter in every service and repository method.

---

## 🗄️ 2. Entity Framework Core & Transactions

- [x] **Transaction Capture Pattern**: Every multi-write operation uses `_unitOfWork.ExecuteTransactionAsync` with result assignment **inside** the lambda:
  ```csharp
  AlertRuleResponse result = null!;
  await _unitOfWork.ExecuteTransactionAsync(async () => {
      var updated = await _repo.UpdateAsync(entity);
      result = MapToResponse(updated);
  }, ct);
  return result;
  ```
- [x] **Read-Only Queries**: All read-only EF Core LINQ queries specify `.AsNoTracking()`.
- [x] **No Direct DbContext Injections**: Services inject `IUnitOfWork` or specific repositories, never `AppDbContext` directly.

---

## 🧪 3. Unit & Integration Testing Standards

- [x] **Test Coverage**: Happy path, not found, and transaction execution counts are verified for all service methods.
- [x] **Mock Delegate Invocation**: `ExecuteTransactionAsync` mocks invoke the delegate parameter:
  ```csharp
  _uowMock.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
      .Returns<Func<Task>, CancellationToken>((action, _) => action());
  ```
- [x] **Zero Thread.Sleep**: No `Thread.Sleep` calls allowed in test suites.

---

## 📝 4. API Response Standards & Error Handling

- [x] **Global Error Middleware**: `GlobalExceptionMiddleware` catches `UserFriendlyException` and returns standardized problem details JSON:
  ```json
  {
    "status": 404,
    "title": "NotFound",
    "detail": "Stock listing for 'INVALID' was not found."
  }
  ```
- [x] **Thin Controllers**: Controllers contain no business logic; they delegate directly to Application-layer services.
