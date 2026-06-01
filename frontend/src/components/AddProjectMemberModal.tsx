import React, { useState, useEffect } from "react";
import { API_BASE_URL } from "../config";
import { message as antdMessage } from "antd";

interface AddProjectMemberModalProps {
  open: boolean;
  onClose: () => void;
  projectId: string | number;
  onMemberAdded?: () => void;
}

export default function AddProjectMemberModal({
  open,
  onClose,
  projectId,
  onMemberAdded,
}: AddProjectMemberModalProps) {
  const [workers, setWorkers] = useState<any[]>([]);
  const [selectedUserId, setSelectedUserId] = useState("");
  const [assignedAs, setAssignedAs] = useState("Worker");
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!open) return;
    fetchAvailableWorkers();
  }, [open]);

  const fetchAvailableWorkers = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem("token");
      const res = await fetch(`${API_BASE_URL}/api/users`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      const data = await res.json();
      // Filter out Admins and already-added members (optional)
      setWorkers(data.filter((u: any) => u.roleID !== 1));
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async () => {
    if (!selectedUserId) return;

    setSubmitting(true);
    try {
      const token = localStorage.getItem("token");
      const res = await fetch(`${API_BASE_URL}/api/projects/${projectId}/members`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(token && { Authorization: `Bearer ${token}` }),
        },
        body: JSON.stringify({
          userID: selectedUserId,
          assignedAs: assignedAs,
        }),
      });

      if (!res.ok) {
        throw new Error(await res.text());
      }

      antdMessage.success("Worker added to the project.");
      onMemberAdded?.();
      onClose();
    } catch (err: any) {
      antdMessage.error("Failed to add member: " + err.message);
    } finally {
      setSubmitting(false);
    }
  };

  const handleCopyCode = async (value: string) => {
    try {
      await navigator.clipboard.writeText(value);
      alert("Code copied to clipboard.");
    } catch {
      alert("Failed to copy code.");
    }
  };

  if (!open) return null;

  return (
    <div className="action-modal-overlay" onClick={onClose}>
      <div className="login-card" onClick={(e) => e.stopPropagation()}>
        <h1 className="login-title">Add Team Member</h1>

        <div className="signinText">
          <select
            className="login-input"
            value={selectedUserId}
            onChange={(e) => setSelectedUserId(e.target.value)}
          >
            <option value="">Select a worker...</option>
            {workers.map((w) => (
              <option key={w.id} value={w.id}>
                {w.fullName} — {w.email}
              </option>
            ))}
          </select>

          <select
            className="login-input signup-role-select"
            value={assignedAs}
            onChange={(e) => setAssignedAs(e.target.value)}
          >
            <option value="Worker">Worker</option>
            <option value="Foreman">Foreman</option>
          </select>
        </div>

        <div className="login-button-row">
          <button
            className="login-cancel-btn"
            onClick={onClose}
          >
            Cancel
          </button>
          <button
            className="login-submit-btn"
            onClick={handleSubmit}
            disabled={submitting || !selectedUserId}
          >
            {submitting ? "Adding..." : "Add to Project"}
          </button>
        </div>
      </div>
    </div>
  );
}