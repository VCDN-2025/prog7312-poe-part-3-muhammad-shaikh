# Municipal Services SA – PROG7312 PoE (Parts 1–3)

A C# .NET Framework Windows Forms application that helps South African citizens:

- **Report municipal issues** (e.g. sanitation, roads, water).
- **Browse local events and announcements**.
- **Track the status of service requests** using advanced data structures.

The project is built for Visual Studio 2022 as a **Windows Forms (.NET Framework)** app and is designed to demonstrate custom data structures (lists, stacks, queues, priority queues, dictionaries, sets, trees, heaps and graphs) in a practical municipal scenario.

---

## 1. How to Compile and Run

### 1.1 Prerequisites

- **Operating System**: Windows 10 or later.
- **IDE**: Visual Studio 2022.
- **Workload**: “.NET desktop development” workload installed.
- **Framework**: .NET Framework 4.x (the solution targets the standard WinForms .NET Framework template in VS 2022).

### 1.2 Opening the Project

1. Clone the repository:
   ```bash
   git clone https://github.com/<your-org-or-user>/prog7312-poe-part-1-muhammad-shaikh.git

2. In Visual Studio 2022:

- File → Open → Project/Solution…

- Browse to the cloned folder and open MunicipalServicesApp.sln.


### 1.3 Building

In Visual Studio 2022:

1. Go to **File → Open → Project/Solution…**
2. Browse to the cloned folder and open **`MunicipalServicesApp.sln`**.

To build:

1. Select configuration **Debug** and **Any CPU** (or your chosen platform).
2. Click **Build → Build Solution** (or press `Ctrl+Shift+B`).
3. Ensure there are no compilation errors.

---

### 1.4 Running

**From Visual Studio:**

- Press **`F5`** (Run with debugger) or **`Ctrl+F5`** (Run without debugging).

**From the compiled EXE (after a successful build):**

1. Navigate to:
   - `bin\Debug\` (or `bin\Release\` if you built Release).
2. Double-click **`MunicipalServicesApp.exe`**.

When the app starts, you will see the **Main Menu** (card-based UI with logo and three tiles).

---

## 2. Application Overview & Usage

### 2.1 Main Menu

The main menu is a **card-based Windows Form** with municipality-style branding and a logo.

Cards:

- **Report Issues**
- **Local Events & Announcements**
- **Service Request Status**

Clicking each card opens the corresponding feature form.

---

### 2.2 Report Issues (Part 1)

**Form:** `ReportIssueForm`

**What you can do:**

- Enter **Location**.
- Select **Category** (Sanitation, Roads & Stormwater, Water & Utilities, Electricity, etc.).
- Write a **Description** (RichTextBox with live character counter).
- Attach **images/documents** via `OpenFileDialog` (JPG, PNG, PDF).
- See an **engagement progress bar** (encouraging messages as you complete fields).
- Submit the issue and see:
  - Auto-generated **reference number**: `MUN-YYYYMMDD-####`.
  - **Submission time**.
  - **Number of issues** stored in this session.

**Where requests go:**

- Saved in memory in the custom `IssueRepository` using `SimpleLinkedList<IssueReport>`.
- Also indexed into other Part 3 data structures (BST, heap, graph) for the **Service Request Status** page.

---

### 2.3 Local Events & Announcements (Part 2)

**Form:** `EventsForm`

**Features:**

- **Filter events** by:
  - Category (e.g. Utilities, Transport, Culture).
  - Date range (**From / To**).
  - Search text (title, description, location).
- **Sort** events by:
  - Date
  - Category
  - Name
- View **recommendations** (“Recommended for you”) based on your past searches.
- View a basic **Announcements** section (not filtered, just informational).

**Data structures used here (brief):**

- `SortedDictionary<DateTime, List<Event>>` → events **by date**.
- `Dictionary<string, Event>` → quick **ID → Event** lookup.
- `HashSet<EventCategory>` → unique **categories** for filter dropdown.
- `Stack<Event> LastViewed` → stack of last-viewed events (history).
- `Queue<Event> NewSubmissions` → queue of new events (demo of queue usage).
- `SimplePriorityQueue<Event>` → priority queue ordered by **event date**.
- `SearchTracker` → records search behaviour (keywords, date buckets, categories) to power recommendations.

---

### 2.4 Service Request Status (Part 3)

**Form:** `ServiceStatusForm`

Main focus of Part 3: shows how advanced data structures manage and display service request information efficiently.

**What you can do:**

#### Search by reference

- Type a reference like `MUN-20251110-0001`.
- Click **Find**:
  - The app first searches the **Binary Search Tree** (`ServiceRequestTree`) for an exact match.
  - If not found, it falls back to a **partial scan** over all references.

#### Browse all requests

The top list shows every request:

- Reference  
- Location  
- Category  
- Status (e.g. “Received”, “In Progress”, “Closed”)  
- Created date/time  
- Short summary  

#### See “Related requests (graph traversal)”

- When you select a request, the middle list shows **related requests** found via the **graph** (same area, same category, etc.).

#### See “Oldest requests (heap view)”

- The bottom list shows the **oldest open requests** (from a custom **min-heap**), useful to prioritise backlogs.

#### Graph summary (MST)

A label like:

> `Graph: 15 node(s), MST edges: 14.`

shows:

- number of nodes (requests in the graph)
- number of edges in the **Minimum Spanning Tree** (should be `N − 1`)

---

## 3. Custom Data Structures and Their Roles (Service Request Status)

This section explains each **custom-built data structure** used in the Service Request Status feature and how it improves efficiency.

### 3.1 `SimpleLinkedList<T>` (used across the app)

**Where:**  
`IssueRepository`, attachments list in `ReportIssueForm`, etc.

**What:**  
A minimal custom singly linked list (node with `Value` and `Next`).

**Why:**

- Satisfies the requirement to use **custom collections**, not `List<T>`.
- Allows simple iteration (`ForEach`) over issues and attachments.

**Complexity:**

- Append: `O(1)` (if tracking tail) / `O(n)` if scanning to end.
- Traversal: `O(n)`.

**Example usage:**

- `IssueRepository` stores all `IssueReport` objects in a `SimpleLinkedList<IssueReport>` and exposes  
  `ForEach(Action<IssueReport>)` for all screens to iterate through service requests.

---

### 3.2 `ServiceRequestTree` – Binary Search Tree (BST) over References

**Type:** Custom **Binary Search Tree** (BST).

**Where:**

- `IssueRepository.ByReference` holds a `ServiceRequestTree`.
- `ServiceStatusForm.BtnFind_Click` calls `IssueRepository.ByReference.Find(refText)`.

**What it stores:**

Each node contains:

- `Key`: the reference string (e.g. `MUN-20251112-0003`).
- `Value`: the corresponding `IssueReport`.

**Operations:**

- `Insert(key, value)` – used when a new issue is submitted.
- `Find(key)` – retrieves the matching `IssueReport` in `O(h)` time (h = tree height).

**Why it helps:**

- Without the BST, searching by reference would require scanning all items: **`O(n)`**.
- With the BST, if it stays reasonably balanced, lookup is about **`O(log n)`**.
- For a municipality with thousands of requests, this makes reference lookups much faster.

**Example in the app (user flow):**

1. User submits a request from **Report Issues**.
2. `IssueRepository.Add()`:
   - Adds the report to `SimpleLinkedList<IssueReport>`.
   - Inserts it into `ServiceRequestTree` keyed by `Reference`.
3. On **Service Request Status**, user types the reference and clicks **Find**.
4. The app does:
   ```csharp
   IssueReport exact = IssueRepository.ByReference.Find(refText);

5. If found, the result appears instantly as a **BST exact match**.

This shows clear use of **Basic Trees / Binary Trees / Binary Search Trees** for fast organisation and retrieval.

---

### 3.3 `IssueMinHeap` + `IssuePriorityHelper` – Min-Heap of Oldest Requests

**Type:** Custom **min-heap** (binary heap).

**Where:**

- `IssuePriorityHelper.BuildHeapFromRepository()` builds a heap of all issues ordered by `CreatedAt`.
- `IssuePriorityHelper.GetOldest(int count)` pops the oldest `count` items.
- `ServiceStatusForm.LoadOldestRequests()` uses this to populate the **“Oldest requests (heap view)”** `ListView`.

**What it stores:**

- Heap array of `IssueReport` items.
- The root is the **oldest request** (minimum `CreatedAt`).

**Operations:**

- `Insert(report)` – percolate up (**heapify-up**) in `O(log n)`.
- `ExtractMin()` – remove oldest report in `O(log n)`.

**Why it helps:**

- A municipality might want to **prioritise very old unresolved issues**.
- Without a heap, you would have to **sort the entire list** every time or **scan linearly**.

With a min-heap:

- Building once: `O(n)`.
- Each “get next oldest” is `O(log n)`.

Efficient to show, for example, the **5 oldest** outstanding issues.

**Example in the app:**

- When the **Service Request Status** form opens:
  - It builds a heap from current issues.
  - Shows the **five oldest** in the “Oldest requests (heap view)” block.

This demonstrates practical **prioritisation using a heap**.

---

### 3.4 `ServiceRequestGraph` – Graph, Traversal & MST

**Type:** Custom **graph** of requests.

**Where:**

- Built via `ServiceRequestGraph.BuildFromRepository()`.
- Used in `ServiceStatusForm.RebuildGraph()` and `UpdateRelated()`.

**Nodes:**

- Each node represents an `IssueReport` (one service request).

**Edges:**

Edges are added between **related requests**, for example:

- Same **category** (e.g. both are “Water & Utilities”).
- Same or similar **location** (e.g. same street / ward).

Each edge may have a **weight** (e.g. how similar they are).

---

#### Graph Traversal – Related Requests

**Method:** `GetRelatedByReference(reference, maxCount)`

- Performs a traversal (e.g. BFS/DFS-like) from the node matching the reference.
- Collects **nearby / related** nodes.

**In UI:**

- When the user selects a request in the top list:
  - `UpdateRelated(report)` calls `GetRelatedByReference`.
  - The **“Related requests (graph traversal)”** list shows those neighbours.

This demonstrates **graph traversal** to find connected items.

---

#### Minimum Spanning Tree (MST)

**Method:** `ComputeMstOrder()`

- Computes a **Minimum Spanning Tree** (MST) over the graph (e.g. Prim’s or Kruskal’s algorithm).
- Results are used to summarise in the label:

  > `Graph: N node(s), MST edges: N-1.`

**Why MST is relevant:**

Conceptually, an MST connects all service requests in the **“cheapest”** way based on how similar they are.

This can be used to:

- Identify **clusters** of similar complaints.
- Visualise how tightly issues are grouped.

**Why this satisfies the requirement:**

- You build a non-trivial **graph structure** over the service requests.
- You **traverse** it to show related requests.
- You compute an **MST** and show statistics to prove the algorithm runs on real data.

**Dependency Handling: Main Issue → Dependent Requests**

In real municipal operations, a single infrastructure fault (e.g., a burst water main) can generate many individual service requests (“no water” complaints). The app models this with main issues and dependent requests:

- Each IssueReport may have:

- IsMainIssue : bool

ParentReference : string?
(null for main issues; set to the main issue’s reference for dependents)

On the Service Request Status page, resolving a main issue automatically updates all dependents with the same ParentReference to Resolved (cascade close).

The graph view links related items (e.g., same category/area); dependents appear directly connected to their main issue.

---

### 3.5 Other Supporting Structures Used in the App

Even though the focus here is on **Service Request Status**, the app also demonstrates:

- `SortedDictionary<DateTime, List<Event>>` → sorted events store (Local Events).
- `Dictionary<string, Event>` → `O(1)` event lookup by ID.
- `HashSet<EventCategory>` → unique categories for filters.
- `Stack<Event>` (`LastViewed`) → “last viewed” history of events.
- `Queue<Event>` (`NewSubmissions`) → FIFO handling of event submissions.
- `SimplePriorityQueue<Event>` → upcoming events by date.
- `SearchTracker` → accumulates user search patterns (keywords, date buckets) for recommendations.

These support the full rubric across **Parts 1–3**.

---

## 4. Changelog (Improvements from Part 1 & 2)

### From Part 1 → now

#### Main Menu UI

- Upgraded from a simple three-button form to a **card-based, branded dashboard**.
- Added municipality-style **logo** and colour scheme.
- Added **“How it works”** strip with step-by-step badges.

#### Report Issues

- Added live **character counter** and input validation with `ErrorProvider`.
- Added dynamic **engagement progress bar** with encouraging status messages.
- Improved attachment handling with validation and **custom linked list** for storage.
- Ensured all data stored in `IssueRepository` uses custom `SimpleLinkedList<IssueReport>` and a custom reference generator (`ReferenceGenerator`).

---

### From Part 2 → now

#### Local Events & Announcements

- Implemented full **search, filter, sort and recommendation** logic.
- Used `SortedDictionary`, `Dictionary`, `HashSet`, `Stack`, `Queue`, and `SimplePriorityQueue<Event>` to manage events efficiently.
- Added a basic **Announcements** section (not filtered, as per requirements).

#### Service Request Status (new in Part 3)

- Added `ServiceStatusForm` with **three full-width sections**:
  - **All requests** (main list).
  - **Related requests** (graph traversal).
  - **Oldest requests** (heap view).

- Implemented and wired:
  - `ServiceRequestTree` (**Binary Search Tree**) for fast reference lookups.
  - `IssueMinHeap` (**min-heap**) with `IssuePriorityHelper` to show oldest requests.
  - `ServiceRequestGraph` (**graph**) with:
    - Graph traversal to find related requests.
    - `ComputeMstOrder()` to calculate a **Minimum Spanning Tree** and display stats.

- Seeded **sample data** (via `SampleIssueSeeder.Seed()` and `IssueRepository`)


## References

Albahari, J. and Albahari, B. (2022) *C# 10 in a Nutshell: The Definitive Reference*. 1st edn. Sebastopol, CA: O’Reilly Media.  
— General C# and .NET language / library reference (collections, LINQ, etc.).

Liberty, J. and MacDonald, M. (2014) *Programming C# 5.0: Building Windows 8, Web, and Desktop Applications*. 6th edn. Sebastopol, CA: O’Reilly Media.  
— Used as background for Windows Forms and desktop app patterns.

Microsoft (2025) *Windows Forms documentation*. Available at: https://learn.microsoft.com/dotnet/desktop/winforms/ (Accessed: 12 November 2025).  
— Official documentation for designing and handling events in Windows Forms.

Weiss, M.A. (2014) *Data Structures and Algorithm Analysis in C++*. 4th edn. Harlow: Pearson.  
— Conceptual reference for binary search trees, heaps and graph algorithms (traversals, MST).

Skiena, S.S. (2020) *The Algorithm Design Manual*. 3rd edn. Cham: Springer.  
— Background on graph algorithms, shortest paths and minimum spanning trees used to inspire the service request graph/MST logic.

Chacon, S. and Straub, B. (2014) *Pro Git*. 2nd edn. New York: Apress. Available at: https://git-scm.com/book/en/v2 (Accessed: 12 November 2025).  
— Used for understanding Git/GitHub workflow for version control and project hosting.


