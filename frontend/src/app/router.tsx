import { createBrowserRouter, Navigate } from "react-router-dom";

import AdminDashboard from "../features/admin/AdminDashboard";
import ExamsPage from "../features/admin/ExamsPage";
import QuestionBankPage from "../features/admin/QuestionBankPage";
import StudentsPage from "../features/admin/StudentsPage";
import LoginPage from "../features/auth/LoginPage";
import ExamTakingPage from "../features/exam/ExamTakingPage";
import StudentDashboard from "../features/student/StudentDashboard";
import AdminLayout from "../layouts/AdminLayout";
import ExamLayout from "../layouts/ExamLayout";
import StudentLayout from "../layouts/StudentLayout";

export const router = createBrowserRouter([
  { path: "/", element: <Navigate to="/login" replace /> },
  { path: "/login", element: <LoginPage /> },
  {
    path: "/admin",
    element: <AdminLayout />,
    children: [
      { index: true, element: <AdminDashboard /> },
      { path: "questions", element: <QuestionBankPage /> },
      { path: "exams", element: <ExamsPage /> },
      { path: "candidates", element: <StudentsPage /> },
      { path: "students", element: <Navigate to="/admin/candidates" replace /> },
      { path: "*", element: <Navigate to="/admin" replace /> },
    ],
  },
  {
    path: "/candidate",
    element: <StudentLayout />,
    children: [
      { index: true, element: <StudentDashboard /> },
      { path: "*", element: <Navigate to="/candidate" replace /> },
    ],
  },
  { path: "/student/*", element: <Navigate to="/candidate" replace /> },
  {
    path: "/exam",
    element: <ExamLayout />,
    children: [
      { index: true, element: <Navigate to="session" replace /> },
      { path: "session", element: <ExamTakingPage /> },
    ],
  },
  { path: "*", element: <Navigate to="/login" replace /> },
]);
