using System;
using System.Drawing;
using System.Windows.Forms;
using MunicipalServicesApp.Domain;

namespace MunicipalServicesApp
{
    /// <summary>
    /// Service Request Status page (Part 3).
    /// Uses:
    /// - Binary Search Tree (ServiceRequestTree) for fast reference lookup
    /// - Min-Heap (IssueMinHeap) to show the oldest requests
    /// - Graph (ServiceRequestGraph) for related requests + MST summary
    /// </summary>
    public class ServiceStatusForm : Form
    {
        private TextBox txtReference;
        private Button btnFind;
        private Button btnClear;

        private ListView lvRequests;
        private Label lblHeader;
        private Label lblSubtitle;
        private Label lblCount;

        private ListView lvOldest;
        private Label lblOldest;

        private ListView lvRelated;
        private Label lblRelated;
        private Label lblGraphInfo;

        private ServiceRequestGraph _graph;

        public ServiceStatusForm()
        {
            InitializeComponent();
            LoadAllRequests();
            LoadOldestRequests();
            RebuildGraph();
        }

        private void InitializeComponent()
        {
            Text = "Service Request Status";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(980, 680);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            // Header
            lblHeader = new Label
            {
                Text = "Service Request Status",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(10, 110, 189),
                Location = new Point(24, 20)
            };

            lblSubtitle = new Label
            {
                Text = "Enter your reference number or browse the list to track progress.",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Location = new Point(28, 56)
            };

            // Search row
            var lblRef = new Label
            {
                Text = "Reference:",
                AutoSize = true,
                Location = new Point(24, 100)
            };

            txtReference = new TextBox
            {
                Location = new Point(100, 96),
                Width = 260
            };

            btnFind = new Button
            {
                Text = "Find",
                Location = new Point(370, 94),
                Width = 80
            };
            btnFind.Click += BtnFind_Click;

            btnClear = new Button
            {
                Text = "Clear",
                Location = new Point(460, 94),
                Width = 80
            };
            btnClear.Click += (s, e) =>
            {
                txtReference.Clear();
                LoadAllRequests();
                LoadOldestRequests();
                RebuildGraph();
            };

            lblCount = new Label
            {
                Text = "0 request(s)",
                AutoSize = true,
                Location = new Point(24, 136),
                ForeColor = Color.FromArgb(70, 70, 70)
            };

            // === MAIN REQUEST LIST (TOP BLOCK, FULL WIDTH) ===
            lvRequests = new ListView
            {
                Location = new Point(24, 164),
                Size = new Size(920, 220),
                View = View.Details,
                FullRowSelect = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            // Columns add up to 920 to avoid horizontal scroll
            lvRequests.Columns.Add("Reference", 140);
            lvRequests.Columns.Add("Location", 260);
            lvRequests.Columns.Add("Category", 130);
            lvRequests.Columns.Add("Status", 110);
            lvRequests.Columns.Add("Created At", 140);
            lvRequests.Columns.Add("Summary", 140);

            lvRequests.DoubleClick += (s, e) =>
            {
                if (lvRequests.SelectedItems.Count == 0) return;
                var report = lvRequests.SelectedItems[0].Tag as IssueReport;
                if (report == null) return;

                MessageBox.Show(
                    "Reference: " + report.Reference + Environment.NewLine +
                    "Location: " + report.Location + Environment.NewLine +
                    "Category: " + report.Category + Environment.NewLine +
                    "Status: " + report.Status + Environment.NewLine +
                    "Created: " + report.CreatedAt.ToString("yyyy-MM-dd HH:mm") + Environment.NewLine +
                    Environment.NewLine +
                    report.Description,
                    "Request details",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };
            lvRequests.SelectedIndexChanged += LvRequests_SelectedIndexChanged;

            // === RELATED REQUESTS (MIDDLE BLOCK, FULL WIDTH) ===
            lblRelated = new Label
            {
                Text = "Related requests (graph traversal)",
                AutoSize = true,
                Location = new Point(24, 392),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            lvRelated = new ListView
            {
                Location = new Point(24, 418),
                Size = new Size(920, 100),
                View = View.Details,
                FullRowSelect = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            lvRelated.Columns.Add("Reference", 140);
            lvRelated.Columns.Add("Category", 130);
            lvRelated.Columns.Add("Status", 110);
            lvRelated.Columns.Add("Location", 260);
            lvRelated.Columns.Add("Summary", 280);

            // Graph info under related block
            lblGraphInfo = new Label
            {
                Text = "Graph: n/a",
                AutoSize = true,
                Location = new Point(24, 524),
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            // === OLDEST REQUESTS (BOTTOM BLOCK, FULL WIDTH) ===
            lblOldest = new Label
            {
                Text = "Oldest requests (heap view)",
                AutoSize = true,
                Location = new Point(24, 548),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            lvOldest = new ListView
            {
                Location = new Point(24, 574),
                Size = new Size(920, 80),
                View = View.Details,
                FullRowSelect = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            lvOldest.Columns.Add("Reference", 140);
            lvOldest.Columns.Add("Location", 260);
            lvOldest.Columns.Add("Category", 130);
            lvOldest.Columns.Add("Status", 110);
            lvOldest.Columns.Add("Created At", 140);
            lvOldest.Columns.Add("Summary", 140);

            Controls.Add(lblHeader);
            Controls.Add(lblSubtitle);
            Controls.Add(lblRef);
            Controls.Add(txtReference);
            Controls.Add(btnFind);
            Controls.Add(btnClear);
            Controls.Add(lblCount);

            Controls.Add(lvRequests);
            Controls.Add(lblRelated);
            Controls.Add(lvRelated);
            Controls.Add(lblGraphInfo);
            Controls.Add(lblOldest);
            Controls.Add(lvOldest);
        }

        // ===== DATA LOADING & LOGIC =====

        private void LoadAllRequests()
        {
            lvRequests.BeginUpdate();
            lvRequests.Items.Clear();

            IssueRepository.ForEach(report =>
            {
                lvRequests.Items.Add(MakeItem(report));
            });

            lvRequests.EndUpdate();
            lblCount.Text = $"{lvRequests.Items.Count} request(s)";
        }

        private void BtnFind_Click(object sender, EventArgs e)
        {
            string refText = (txtReference.Text ?? "").Trim();

            if (refText.Length == 0)
            {
                LoadAllRequests();
                LoadOldestRequests();
                RebuildGraph();
                return;
            }

            lvRequests.BeginUpdate();
            lvRequests.Items.Clear();

            // 1) BST exact lookup
            IssueReport exact = IssueRepository.ByReference.Find(refText);
            if (exact != null)
            {
                lvRequests.Items.Add(MakeItem(exact));
                lvRequests.EndUpdate();
                lblCount.Text = $"1 request(s) matching \"{refText}\" (BST exact match)";
                UpdateRelated(exact);
                return;
            }

            // 2) Fallback: partial scan
            IssueRepository.ForEach(report =>
            {
                if (report.Reference != null &&
                    report.Reference.IndexOf(refText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lvRequests.Items.Add(MakeItem(report));
                }
            });

            lvRequests.EndUpdate();
            lblCount.Text = $"{lvRequests.Items.Count} request(s) matching \"{refText}\" (partial scan)";

            if (lvRequests.Items.Count > 0)
            {
                var r = lvRequests.Items[0].Tag as IssueReport;
                if (r != null) UpdateRelated(r);
            }
        }

        private ListViewItem MakeItem(IssueReport report)
        {
            var item = new ListViewItem(report.Reference ?? "");
            item.SubItems.Add(report.Location ?? "");
            item.SubItems.Add(report.Category ?? "");
            item.SubItems.Add(report.Status ?? "");
            item.SubItems.Add(report.CreatedAt.ToString("yyyy-MM-dd HH:mm"));

            string summary = (report.Description ?? "");
            if (summary.Length > 80) summary = summary.Substring(0, 80) + "…";
            item.SubItems.Add(summary);

            item.Tag = report;
            return item;
        }

        private void LoadOldestRequests()
        {
            if (lvOldest == null) return;

            lvOldest.BeginUpdate();
            lvOldest.Items.Clear();

            IssueReport[] oldest = IssuePriorityHelper.GetOldest(5);

            if (oldest.Length == 0)
            {
                lvOldest.Items.Add(
                    new ListViewItem(new[] { "", "No requests yet.", "", "", "", "" })
                );
            }
            else
            {
                foreach (var r in oldest)
                {
                    var item = new ListViewItem(r.Reference ?? "");
                    item.SubItems.Add(r.Location ?? "");
                    item.SubItems.Add(r.Category ?? "");
                    item.SubItems.Add(r.Status ?? "");
                    item.SubItems.Add(r.CreatedAt.ToString("yyyy-MM-dd HH:mm"));

                    string summary = (r.Description ?? "");
                    if (summary.Length > 80) summary = summary.Substring(0, 80) + "…";
                    item.SubItems.Add(summary);

                    item.Tag = r;
                    lvOldest.Items.Add(item);
                }
            }

            lvOldest.EndUpdate();
        }

        private void RebuildGraph()
        {
            _graph = ServiceRequestGraph.BuildFromRepository();

            if (_graph == null || _graph.Count == 0)
            {
                lblGraphInfo.Text = "Graph: no requests yet.";
                return;
            }

            var mst = _graph.ComputeMstOrder();
            int nodeCount = _graph.Count;
            int edgeCount = mst.Length > 0 ? mst.Length - 1 : 0;

            lblGraphInfo.Text = $"Graph: {nodeCount} node(s), MST edges: {edgeCount}.";
        }

        private void LvRequests_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvRequests.SelectedItems.Count == 0)
            {
                lvRelated.Items.Clear();
                return;
            }

            var report = lvRequests.SelectedItems[0].Tag as IssueReport;
            if (report == null) return;

            UpdateRelated(report);
        }

        private void UpdateRelated(IssueReport report)
        {
            if (_graph == null || _graph.Count == 0)
            {
                lvRelated.Items.Clear();
                return;
            }

            lvRelated.BeginUpdate();
            lvRelated.Items.Clear();

            IssueReport[] related = _graph.GetRelatedByReference(report.Reference, 5);

            if (related.Length == 0)
            {
                lvRelated.Items.Add(
                    new ListViewItem(new[] { "", "No related requests", "", "", "" })
                );
            }
            else
            {
                foreach (var r in related)
                {
                    if (r.Reference == report.Reference) continue; // skip self

                    string summary = (r.Description ?? "");
                    if (summary.Length > 60) summary = summary.Substring(0, 60) + "…";

                    var item = new ListViewItem(r.Reference ?? "");
                    item.SubItems.Add(r.Category ?? "");
                    item.SubItems.Add(r.Status ?? "");
                    item.SubItems.Add(r.Location ?? "");
                    item.SubItems.Add(summary);
                    item.Tag = r;
                    lvRelated.Items.Add(item);
                }

                if (lvRelated.Items.Count == 0)
                {
                    lvRelated.Items.Add(
                        new ListViewItem(new[] { "", "No related requests", "", "", "" })
                    );
                }
            }

            lvRelated.EndUpdate();
        }
    }
}
