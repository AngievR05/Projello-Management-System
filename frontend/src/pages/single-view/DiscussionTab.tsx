import React, { useEffect, useState } from "react";
import { API_BASE_URL } from "../../config";

type DiscussionReaction = {
  id: number;
  userId: string;
  userFullName: string;
  emoji: string;
  createdAt: string;
};

type DiscussionComment = {
  id: number;
  userId: string;
  userFullName: string;
  commentText: string;
  createdAt: string;
};

type DiscussionPost = {
  id: number;
  projectId: number;
  userId: string;
  userFullName: string;
  caption: string | null;
  imageUrl: string;
  createdAt: string;
  reactions: DiscussionReaction[];
  comments: DiscussionComment[];
};

interface DiscussionTabProps {
  projectId: number;
}

const reactionOptions = ["👍", "❤️", "😂", "👏", "🔥"];

export default function DiscussionTab({ projectId }: DiscussionTabProps) {
  const [posts, setPosts] = useState<DiscussionPost[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [commentDrafts, setCommentDrafts] = useState<Record<number, string>>({});
  const [submittingId, setSubmittingId] = useState<number | null>(null);

  const fetchPosts = async () => {
    setLoading(true);
    setError(null);

    try {
      const token = localStorage.getItem("token");
      const res = await fetch(`${API_BASE_URL}/api/projects/${projectId}/updates`, {
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      });

      if (!res.ok) throw new Error("Failed to fetch discussion");

      const data: DiscussionPost[] = await res.json();
      setPosts(data);
    } catch (err: any) {
      setError(err.message || "Failed to load discussion");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (projectId) {
      fetchPosts();
    }
  }, [projectId]);

  const handleReact = async (updateId: number, emoji: string) => {
    try {
      setSubmittingId(updateId);
      const token = localStorage.getItem("token");

      const res = await fetch(`${API_BASE_URL}/api/projects/${projectId}/updates/${updateId}/react`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ emoji }),
      });

      if (!res.ok) throw new Error("Failed to save reaction");

      await fetchPosts();
    } catch (err: any) {
      setError(err.message || "Failed to add reaction");
    } finally {
      setSubmittingId(null);
    }
  };

  const handleCommentSubmit = async (updateId: number) => {
    const commentText = commentDrafts[updateId]?.trim();
    if (!commentText) return;

    try {
      setSubmittingId(updateId);
      const token = localStorage.getItem("token");

      const res = await fetch(`${API_BASE_URL}/api/projects/${projectId}/updates/${updateId}/comments`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ commentText }),
      });

      if (!res.ok) throw new Error("Failed to save comment");

      setCommentDrafts((current) => ({ ...current, [updateId]: "" }));
      await fetchPosts();
    } catch (err: any) {
      setError(err.message || "Failed to add comment");
    } finally {
      setSubmittingId(null);
    }
  };

  if (loading) return <p style={{ padding: 20 }}>Loading discussion...</p>;
  if (error) return <p style={{ padding: 20, color: "red" }}>{error}</p>;

  return (
    <div style={{ padding: 16 }}>
      <div style={{ marginBottom: 20 }}>
        <h2 style={{ margin: 0 }}>Discussion</h2>
        <p style={{ margin: "8px 0 0", color: "#64748b" }}>
          Posts, reactions, and comments for this project.
        </p>
      </div>

      {posts.length === 0 ? (
        <p>No posts yet in this discussion.</p>
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
          {posts.map((post) => {
            const reactionCounts = post.reactions.reduce<Record<string, number>>((acc, reaction) => {
              acc[reaction.emoji] = (acc[reaction.emoji] || 0) + 1;
              return acc;
            }, {});

            return (
              <article
                key={post.id}
                style={{
                  background: "#fff",
                  border: "1px solid #e5e7eb",
                  borderRadius: 12,
                  overflow: "hidden",
                }}
              >
                {post.imageUrl && (
                  <img
                    src={post.imageUrl}
                    alt="Post"
                    style={{
                      width: "100%",
                      maxHeight: 320,
                      objectFit: "cover",
                      display: "block",
                    }}
                  />
                )}

                <div style={{ padding: 16 }}>
                  <div style={{ display: "flex", justifyContent: "space-between", gap: 12, marginBottom: 10 }}>
                    <div>
                      <div style={{ fontWeight: 600 }}>{post.userFullName}</div>
                      <div style={{ fontSize: 12, color: "#64748b" }}>
                        {new Date(post.createdAt).toLocaleString()}
                      </div>
                    </div>
                  </div>

                  {post.caption && (
                    <p style={{ margin: "12px 0", fontSize: 15, lineHeight: 1.5 }}>
                      {post.caption}
                    </p>
                  )}

                  <div
                    style={{
                      display: "flex",
                      flexWrap: "wrap",
                      gap: 8,
                      padding: "12px 0",
                      borderTop: "1px solid #f1f5f9",
                      borderBottom: "1px solid #f1f5f9",
                      marginBottom: 12,
                    }}
                  >
                    {reactionOptions.map((emoji) => (
                      <button
                        key={emoji}
                        type="button"
                        onClick={() => handleReact(post.id, emoji)}
                        disabled={submittingId === post.id}
                        style={{
                          border: "1px solid #e5e7eb",
                          background: "#f8fafc",
                          borderRadius: 999,
                          padding: "6px 10px",
                          cursor: "pointer",
                        }}
                      >
                        {emoji}
                        {reactionCounts[emoji] ? ` ${reactionCounts[emoji]}` : ""}
                      </button>
                    ))}
                  </div>

                  <section>
                    <div style={{ fontWeight: 600, marginBottom: 10 }}>
                      Comments {post.comments.length ? `(${post.comments.length})` : ""}
                    </div>

                    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
                      {post.comments.map((comment) => (
                        <div
                          key={comment.id}
                          style={{
                            background: "#f8fafc",
                            border: "1px solid #e5e7eb",
                            borderRadius: 10,
                            padding: 12,
                          }}
                        >
                          <div style={{ display: "flex", justifyContent: "space-between", gap: 12 }}>
                            <strong>{comment.userFullName}</strong>
                            <span style={{ fontSize: 12, color: "#64748b" }}>
                              {new Date(comment.createdAt).toLocaleString()}
                            </span>
                          </div>
                          <p style={{ margin: "8px 0 0", lineHeight: 1.5 }}>{comment.commentText}</p>
                        </div>
                      ))}
                    </div>

                    <form
                      onSubmit={(e) => {
                        e.preventDefault();
                        handleCommentSubmit(post.id);
                      }}
                      style={{ marginTop: 12 }}
                    >
                      <input
                        type="text"
                        value={commentDrafts[post.id] || ""}
                        onChange={(e) =>
                          setCommentDrafts((current) => ({
                            ...current,
                            [post.id]: e.target.value,
                          }))
                        }
                        placeholder="Write a comment..."
                        style={{
                          width: "100%",
                          padding: "10px 12px",
                          border: "1px solid #e5e7eb",
                          borderRadius: 8,
                          fontSize: 14,
                        }}
                      />
                    </form>
                  </section>
                </div>
              </article>
            );
          })}
        </div>
      )}
    </div>
  );
}