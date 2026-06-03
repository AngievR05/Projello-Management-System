global.TextEncoder = require("util").TextEncoder;

import React from "react";
import { render, screen, waitFor, fireEvent, act } from "@testing-library/react";
import "@testing-library/jest-dom";
import Clients from "./Clients";
import { BrowserRouter } from "react-router-dom";

// Mock config module
jest.mock("../../config", () => ({
    API_BASE_URL: "https://localhost5000",
}));

// Mock the child data table to simplify row selection targeting
jest.mock("../../components/ManagementClientTable", () => {
    return function MockTable({ rows, onRowAction, onRowClick }: any) {
        return (
            <table data-testid="management-client-table">
                <tbody>
                    {rows.map((row: any) => (
                        <tr key={row.clientId} onClick={() => onRowClick(row)}>
                            <td>{row.name}</td>
                            <td>{row.company}</td>
                            <td>{row.status}</td>
                            <td>
                                <button 
                                    data-testid={`action-btn-${row.clientId}`} 
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        onRowAction(row);
                                    }}
                                >
                                    Actions
                                </button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        );
    };
});

// Mock external Modals to prevent form input label tracking crashes
jest.mock("../../components/ClientAddModal", () => {
    return function MockAddModal({ open, onClose, onClientAdded }: any) {
        if (!open) return null;
        return (
            <div data-testid="client-add-modal">
                <button onClick={onClientAdded}>Submit Add Client</button>
                <button onClick={onClose}>Close Add Modal</button>
            </div>
        );
    };
});

// FIXED: Mock matching the exact named export structure of ProjectAddModal
jest.mock("../../components/ProjectAddModal", () => {
    return {
        ProjectAddModal: function MockProjectModal({ open, onClose, onSubmit }: any) {
            if (!open) return null;
            return (
                <div data-testid="project-add-modal">
                    <button onClick={() => onSubmit({ name: "Alpha Initiative", description: "Desc", dueDate: "2026-12-31" })}>
                        Submit Project
                    </button>
                    <button onClick={onClose}>Close Project Modal</button>
                </div>
            );
        }
    };
});

describe("ClientsPage Frontend Unit and Coverage Test Suite", () => {
    const mockClientsData = [
        {
            clientId: "101",
            name: "Acme Corp",
            company: "Acme Industries",
            totalPaid: "R 15,000",
            outstanding: "R 0",
            projects: 3,
            activeProjects: "2 active",
            isBlacklisted: false,
        },
        {
            clientId: "102",
            name: "Wayne Enterprises",
            company: "Wayne Industries",
            totalPaid: "R 0",
            outstanding: "R 50,000",
            projects: 1,
            activeProjects: "0 active",
            isBlacklisted: true,
        },
    ];

    const mockSummaryData = {
        totalRevenue: 115000,
        outstanding: 50000,
        activeClients: 1,
        blacklistClients: 1,
    };

    let fetchMock: jest.Mock;

    beforeEach(() => {
        jest.resetAllMocks();
        localStorage.clear();
        window.alert = jest.fn();
        
        fetchMock = jest.fn();
        global.fetch = fetchMock;
    });

    const setupMockUserToken = (roleId: number) => {
        const payload = btoa(JSON.stringify({ RoleID: String(roleId) }));
        const FakeToken = `header.${payload}.signature`;
        localStorage.setItem("token", FakeToken);
    };

    const seedStandardFetchMocks = () => {
        fetchMock.mockImplementation((url: string) => {
            if (url.endsWith("/api/clients")) {
                return Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve(mockClientsData),
                });
            }
            if (url.endsWith("/api/clients/summary")) {
                return Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve(mockSummaryData),
                });
            }
            return Promise.resolve({
                ok: true,
                json: () => Promise.resolve({}),
                text: () => Promise.resolve("Success"),
            });
        });
    };

    test("should successfully hydrate state metrics cards and client table", async () => {
        setupMockUserToken(3);
        seedStandardFetchMocks();

        render(
            <BrowserRouter>
                <Clients />
            </BrowserRouter>
        );

        await waitFor(() => {
            expect(screen.queryByText("Loading clients...")).not.toBeInTheDocument();
        });

        expect(screen.getByText("Total Revenue")).toBeInTheDocument();
        expect(screen.getByText("Outstanding")).toBeInTheDocument();
        expect(screen.getByTestId("management-client-table")).toBeInTheDocument();
    });

    test("Should completely restrict 'Add Client' visibility for unauthorized worker roles", async () => {
        setupMockUserToken(3);
        seedStandardFetchMocks();

        render(
            <BrowserRouter>
                <Clients />
            </BrowserRouter>
        );

        await waitFor(() => {
            expect(screen.queryByText("Loading clients...")).not.toBeInTheDocument();
        });

        expect(screen.queryByRole("button", { name: /add client/i })).not.toBeInTheDocument();
    });

    test("should open and interact with the Add Client mock safely", async () => {
        setupMockUserToken(1);
        seedStandardFetchMocks();

        render(
            <BrowserRouter>
                <Clients />
            </BrowserRouter>
        );

        await waitFor(() => {
            expect(screen.queryByText(/Loading clients.../i)).not.toBeInTheDocument();
        });

        const addClientButton = screen.getByRole("button", { name: /Add Client/i });
        fireEvent.click(addClientButton);

        expect(screen.getByTestId("client-add-modal")).toBeInTheDocument();

        await act(async () => {
            fireEvent.click(screen.getByText("Submit Add Client"));
        });
    });

    test("should execute client blacklist action chain successfully within the real modal", async () => {
        setupMockUserToken(1);
        seedStandardFetchMocks();

        render(
            <BrowserRouter>
                <Clients />
            </BrowserRouter>
        );

        await waitFor(() => {
            expect(screen.queryByText("Loading clients...")).not.toBeInTheDocument();
        });

        const actionBtn = await screen.findByTestId("action-btn-101");
        fireEvent.click(actionBtn);

        expect(screen.getByText("Choose an action below")).toBeInTheDocument();
        
        fireEvent.click(screen.getByRole("button", { name: /Blacklist Client/i }));
        
        const reasonInput = screen.getByPlaceholderText("No reason provided");
        fireEvent.change(reasonInput, { target: { value: "Repeated non-payment" } });

        fetchMock.mockImplementation(() => Promise.resolve({ ok: true, json: () => Promise.resolve({}) }));

        await act(async () => {
            fireEvent.click(screen.getByRole("button", { name: /Confirm Blacklist/i }));
        });

        await waitFor(() => {
            expect(screen.getByText(/has been blacklisted/i)).toBeInTheDocument();
        });

        // FIXED: Using getAllByRole to get the target modal close button specifically
        const closeButtons = screen.getAllByRole("button", { name: /Close/i });
        fireEvent.click(closeButtons[0]);
    });

    test("should handle unblacklisting actions inside the real modal sequence", async () => {
        setupMockUserToken(1);
        seedStandardFetchMocks();

        render(
            <BrowserRouter>
                <Clients />
            </BrowserRouter>
        );

        await waitFor(() => {
            expect(screen.queryByText("Loading clients...")).not.toBeInTheDocument();
        });

        const actionBtn = await screen.findByTestId("action-btn-102");
        fireEvent.click(actionBtn);

        fireEvent.click(screen.getByRole("button", { name: /Remove from Blacklist/i }));
        
        fetchMock.mockImplementation(() => Promise.resolve({ ok: true, json: () => Promise.resolve({}) }));

        await act(async () => {
            fireEvent.click(screen.getByRole("button", { name: /Yes, Remove from Blacklist/i }));
        });

        await waitFor(() => {
            expect(screen.getByText(/has been removed from the blacklist/i)).toBeInTheDocument();
        });
        
        const closeButtons = screen.getAllByRole("button", { name: /Close/i });
        fireEvent.click(closeButtons[0]);
    });

    test("should interact with UI-only status change elements and cancel options", async () => {
        setupMockUserToken(1);
        seedStandardFetchMocks();

        render(
            <BrowserRouter>
                <Clients />
            </BrowserRouter>
        );

        await waitFor(() => {
            expect(screen.queryByText("Loading clients...")).not.toBeInTheDocument();
        });

        const actionBtn = await screen.findByTestId("action-btn-101");
        fireEvent.click(actionBtn);

        fireEvent.click(screen.getByRole("button", { name: /Change Status/i }));
        fireEvent.click(screen.getByRole("button", { name: /^Active$/i }));
        
        // FIXED: Disambiguating multiple "Close" buttons matching here
        const closeButtons = screen.getAllByRole("button", { name: /Close/i });
        fireEvent.click(closeButtons[0]);

        fireEvent.click(await screen.findByTestId("action-btn-101"));
        fireEvent.click(screen.getByRole("button", { name: /Edit Client/i }));
        
        const subsequentCloseButtons = screen.getAllByRole("button", { name: /Close/i });
        fireEvent.click(subsequentCloseButtons[0]);
    });

    test("should handle project creation workflow clicks smoothly and submit form data", async () => {
        setupMockUserToken(1);
        seedStandardFetchMocks();

        render(
            <BrowserRouter>
                <Clients />
            </BrowserRouter>
        );

        await waitFor(() => {
            expect(screen.queryByText("Loading clients...")).not.toBeInTheDocument();
        });

        const actionBtn = await screen.findByTestId("action-btn-101");
        fireEvent.click(actionBtn);
        fireEvent.click(screen.getByRole("button", { name: /Add New Project/i }));

        expect(screen.getByTestId("project-add-modal")).toBeInTheDocument();

        fetchMock.mockImplementation(() => Promise.resolve({ ok: true, text: () => Promise.resolve("Success") }));

        await act(async () => {
            fireEvent.click(screen.getByText("Submit Project"));
        });
    });

    test("should display error messaging blocks layout when client data rejects", async () => {
        setupMockUserToken(1);

        fetchMock.mockImplementation(() =>
            Promise.resolve({
                ok: false,
                text: () => Promise.resolve("Database Error Encountered"),
            })
        );

        render(
            <BrowserRouter>
                <Clients />
            </BrowserRouter>
        );

        await waitFor(() => {
            expect(screen.getByText(/Error: Database Error Encountered/i)).toBeInTheDocument();
        });
    });
});