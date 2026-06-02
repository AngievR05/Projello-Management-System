import {TextEncoder, TextDecoder} from 'util';
global.TextEncoder = TextEncoder;
global.TextDecoder = TextDecoder as any;

import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import ManagementPage from "./management";

// mock hooks and sub-comps
    const mockNavigate = jest.fn();
    jest.mock("react-router-dom", () => ({
        ...jest.requireActual("react-router-dom"),
        useNavigate: () => mockNavigate,
    }));

    // mock the inner views to keep this suite isolated to management page behavior
    jest.mock("./Clients", () => ({
        __esModule: true,
        default: () => <div data-testid="clients-view">Clients View</div>
    }));

    jest.mock("./Workers", () => ({
        __esModule: true,
        default: () => <div data-testid="workers-view">Workers View</div>
    }));

    // mocking atomic presentation comps
    jest.mock("../../components/ManagementTopNav", () => ({
    __esModule: true,
    default: ({ tabs, activeTab, onTabChange }: any) => (
        <nav data-testid="top-nav">
        {tabs.map((tab: any) => (
            <button
            key={tab.id}
            data-testid={`tab-${tab.id}`}
            className={activeTab === tab.id ? "active" : ""}
            onClick={() => onTabChange(tab.id)}
            >
            {tab.label}
            </button>
        ))}
        </nav>
    ),
    }));

    jest.mock("../../components/ManagementClientTable", () => ({
    __esModule: true,
    default: ({ rows, onRowClick }: any) => (
        <table data-testid="projects-table">
        <tbody>
            {rows.map((row: any) => (
            <tr key={row.clientId} onClick={() => onRowClick(row)} data-testid="project-row">
                <td>{row.initials}</td>
                <td>{row.name}</td>
                <td>{row.company}</td>
                <td>{row.status}</td>
            </tr>
            ))}
        </tbody>
        </table>
    ),
    }));

    // test specs

    describe("ManagementPage Component", () => {
  const mockProjects = [
    {
      projectID: 101,
      name: "Warehouse Construction",
      description: "Building foundation",
      clientID: 5,
      clientName: "Build Corp",
      status: "Active",
      startDate: null as string | null,
      dueDate: null as string | null,
      createdAt: "2026-05-01",
    },
    {
      projectID: 102,
      name: "Office Refurbishment",
      description: "Painting rooms",
      clientID: 9,
      clientName: "Apex Industries",
      status: "Completed",
      startDate: null as string | null,
      dueDate: null as string | null,
      createdAt: "2026-05-15",
    },
  ];

  const mockClientsLookup = [
    { clientID: 5, name: "Build Corp" },
    { clientID: 9, name: "Apex Industries" },
  ];

  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();

    // Universal multi-endpoint fetch simulation
    global.fetch = jest.fn().mockImplementation((url: string) => {
      if (url.endsWith("/api/projects")) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockProjects),
        });
      }
      if (url.endsWith("/clients")) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockClientsLookup),
        });
      }
      return Promise.reject(new Error(`Unhandled URL: ${url}`));
    }) as jest.Mock;
  });

  it("renders loading initially and fetches project data successfully", async () => {
    render(<ManagementPage />);

    expect(screen.getByText(/Loading projects.../i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.queryByText(/Loading projects.../i)).not.toBeInTheDocument();
    });

    expect(screen.getByTestId("projects-table")).toBeInTheDocument();
    expect(screen.getByText("Warehouse Construction")).toBeInTheDocument();
    expect(screen.getByText("Office Refurbishment")).toBeInTheDocument();
  });

  it("correctly converts client names into compact uppercase initials", async () => {
    render(<ManagementPage />);

    await waitFor(() => {
      expect(screen.queryByText(/Loading projects.../i)).not.toBeInTheDocument();
    });

    // Build Corp -> BC, Apex Industries -> AI
    expect(screen.getByText("BC")).toBeInTheDocument();
    expect(screen.getByText("AI")).toBeInTheDocument();
  });

  it("navigates tabs reactively and displays corresponding sub-pages", async () => {
    render(<ManagementPage />);

    await waitFor(() => {
      expect(screen.queryByText(/Loading projects.../i)).not.toBeInTheDocument();
    });

    // Verify default view is projects
    expect(screen.getByText("Project Management")).toBeInTheDocument();

    // Navigate to Clients
    const clientsTab = screen.getByTestId("tab-clients");
    fireEvent.click(clientsTab);
    expect(screen.getByTestId("clients-view")).toBeInTheDocument();
    expect(screen.queryByText("Project Management")).not.toBeInTheDocument();

    // Navigate to Workers
    const workersTab = screen.getByTestId("tab-workers");
    fireEvent.click(workersTab);
    expect(screen.getByTestId("workers-view")).toBeInTheDocument();
    expect(screen.queryByTestId("clients-view")).not.toBeInTheDocument();
  });

  it("redirects users to single-view routing upon project row click interaction", async () => {
    render(<ManagementPage />);

    await waitFor(() => {
      expect(screen.queryByText(/Loading projects.../i)).not.toBeInTheDocument();
    });

    const rows = screen.getAllByTestId("project-row");
    
    // Click on the first project row (Warehouse Construction with ID 101)
    fireEvent.click(rows[0]);

    expect(mockNavigate).toHaveBeenCalledTimes(1);
    expect(mockNavigate).toHaveBeenCalledWith("/single-view/101");
  });

  it("displays fallback user messages if fetched dataset is empty", async () => {
    // Override fetch mock for this test case specifically
    global.fetch = jest.fn().mockImplementation(() =>
      Promise.resolve({
        ok: true,
        json: () => Promise.resolve([]),
      })
    ) as jest.Mock;

    render(<ManagementPage />);

    await waitFor(() => {
      expect(screen.queryByText(/Loading projects.../i)).not.toBeInTheDocument();
    });

    expect(screen.getByText(/No projects found./i)).toBeInTheDocument();
  });
});