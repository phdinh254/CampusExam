import { createBrowserRouter, Navigate } from "react-router-dom";

import AdminDashboard from "../features/admin/AdminDashboard";
import ExamsPage from "../features/admin/ExamsPage";
import QuestionBankPage from "../features/admin/QuestionBankPage";
import StudentsPage from "../features/admin/StudentsPage";
import LearningOutcomesPage from "../features/learning-outcomes/LearningOutcomesPage";
import QuestionImportPage from "../features/question-bank/QuestionImportPage";
import ChangePasswordPage from "../features/auth/ChangePasswordPage";
import LoginPage from "../features/auth/LoginPage";
import RequireRole, { TrangChuRedirect } from "../features/auth/RequireRole";
import ExamTakingPage from "../features/exam/ExamTakingPage";
import AccountPage from "../features/student/AccountPage";
import { ExamResultsPage, JoinExamPage } from "../features/student/OnlineExamPages";
import StudentDashboard from "../features/student/StudentDashboard";
import AdminLayout from "../layouts/AdminLayout";
import ExamLayout from "../layouts/ExamLayout";
import StudentLayout from "../layouts/StudentLayout";

export const router = createBrowserRouter([
  { path: "/", element: <TrangChuRedirect /> },
  { path: "/login", element: <LoginPage /> },
  {
    element: <RequireRole choPhepPhaiDoiMatKhau />,
    children: [{ path: "/doi-mat-khau", element: <ChangePasswordPage /> }],
  },
  {
    element: <RequireRole vaiTro="ADMIN" />,
    children: [
      {
        path: "/admin",
        element: <AdminLayout />,
        children: [
          { index: true, element: <AdminDashboard /> },
          { path: "learning-outcomes", element: <LearningOutcomesPage /> },
          { path: "questions", element: <QuestionBankPage /> },
          { path: "questions/import", element: <QuestionImportPage /> },
          { path: "exams", element: <ExamsPage /> },
          { path: "candidates", element: <StudentsPage /> },
          { path: "students", element: <Navigate to="/admin/candidates" replace /> },
          { path: "*", element: <Navigate to="/admin" replace /> },
        ],
      },
    ],
  },
  {
    element: <RequireRole vaiTro="THISINH" />,
    children: [
      {
        path: "/candidate",
        element: <StudentLayout />,
        children: [
          { index: true, element: <StudentDashboard /> },
          { path: "thi", element: <Navigate to="/candidate/thi/tham-gia" replace /> },
          { path: "thi/tham-gia", element: <JoinExamPage /> },
          { path: "thi/ket-qua", element: <ExamResultsPage /> },
          { path: "tai-khoan", element: <AccountPage /> },
          { path: "*", element: <Navigate to="/candidate" replace /> },
        ],
      },
      {
        path: "/exam",
        element: <ExamLayout />,
        children: [
          { index: true, element: <Navigate to="session" replace /> },
          { path: "session", element: <ExamTakingPage /> },
        ],
      },
    ],
  },
  { path: "/student/*", element: <Navigate to="/candidate" replace /> },
  { path: "*", element: <TrangChuRedirect /> },
]);
