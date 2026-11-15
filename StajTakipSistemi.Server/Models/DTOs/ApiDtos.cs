using System;
using System.Collections.Generic;

namespace StajTakipSistemi.Server.Models.DTOs
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string TcNo { get; set; } = string.Empty;
        public string Nationality { get; set; } = "TC";
        public string Department { get; set; } = "Yazılım Mühendisliği";
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class DocumentUploadDto
    {
        public string StudentFullName { get; set; } = string.Empty;
        public string StudentTcNo { get; set; } = string.Empty;
        public string Nationality { get; set; } = "TC";
        public string StudentNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FaxNumber { get; set; }

        public string AcademicYear { get; set; } = "2025-2026 Bahar";
        public DateTime SubmissionDate { get; set; } = DateTime.Today;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string SelectedDaysOfWeek { get; set; } = "Pazartesi,Salı,Çarşamba,Perşembe,Cuma";
        public int DeclaredDaysCount { get; set; } = 20;

        public string CompanyName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public int EngineerCount { get; set; } = 1;
        public string? EngineerGraduationDepartment { get; set; }

        public bool HasStudentSignature { get; set; } = false;
        public bool HasAdvisorSignature { get; set; } = false;
        public bool HasEmployerSignatureAndStamp { get; set; } = true;

        public string? GoogleFormUrl { get; set; }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class CheckGoogleFormRequest
    {
        public string Url { get; set; } = string.Empty;
    }
}
