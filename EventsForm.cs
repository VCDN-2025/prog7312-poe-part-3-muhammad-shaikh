using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using MunicipalServicesApp.Domain;

namespace MunicipalServicesApp
{
    // Single-file, code-only WinForms screen (no Designer/resx)
    public class EventsForm : Form
    {
        private ComboBox cboCategory;
        private DateTimePicker dtFrom;
        private DateTimePicker dtTo;
        private TextBox txtSearch;
        private ComboBox cboSort;
        private Button btnApply;
        private Button btnClear;

        private ListView lvEvents;

        // Recommendations UI
        private ListView lvRecommendations;
        private Label lblCount;
        private Label lblReco;

        // NEW: Announcements UI
        private Label lblAnn;
        private ListView lvAnnouncements;

        public EventsForm()
        {
            InitializeComponent();
            if (EventStore.Count < 15) EventSeeder.Seed(); // demo data safeguard
            LoadFilters();
            ApplyFilters();
            LoadRecommendations();
            LoadAnnouncements(); // NEW
        }

        private void InitializeComponent()
        {
            Text = "Local Events & Announcements";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(980, 740);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            // FILTERS ROW (top banner)
            var pnlFilters = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White };

            var lblCat = new Label { Text = "Category:", AutoSize = true, Location = new Point(20, 20) };
            cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(95, 16), Width = 180 };

            var lblFrom = new Label { Text = "From:", AutoSize = true, Location = new Point(300, 20) };
            dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(346, 16), Width = 110 };
            var lblTo = new Label { Text = "To:", AutoSize = true, Location = new Point(470, 20) };
            dtTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(500, 16), Width = 110 };

            var lblSearch = new Label { Text = "Search:", AutoSize = true, Location = new Point(630, 20) };
            txtSearch = new TextBox { Location = new Point(690, 16), Width = 220 };

            var lblSort = new Label { Text = "Sort by:", AutoSize = true, Location = new Point(20, 60) };
            cboSort = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(85, 56), Width = 140 };
            cboSort.Items.AddRange(new object[] { "Date", "Category", "Name" });
            cboSort.SelectedIndex = 0;

            btnApply = new Button { Text = "Apply", Location = new Point(250, 55), Width = 90 };
            btnClear = new Button { Text = "Clear", Location = new Point(350, 55), Width = 90 };

            btnApply.Click += (s, e) => { RecordSearch(); ApplyFilters(); LoadRecommendations(); };
            btnClear.Click += (s, e) =>
            {
                cboCategory.SelectedIndex = 0;
                dtFrom.Value = new DateTime(DateTime.Now.Year, 1, 1);
                dtTo.Value = new DateTime(DateTime.Now.Year, 12, 31);
                txtSearch.Clear();
                cboSort.SelectedIndex = 0;
                ApplyFilters();
                LoadRecommendations();
            };

            // Logo (top-right)
            var picLogo = new PictureBox
            {
                Image = Properties.Resources.municipality_logo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(72, 72),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.White
            };
            picLogo.Location = new Point(pnlFilters.Width - picLogo.Width - 16, 12);
            pnlFilters.Resize += (s, e) =>
            {
                picLogo.Location = new Point(pnlFilters.Width - picLogo.Width - 16, 12);
            };

            pnlFilters.Controls.AddRange(new Control[]
            { lblCat, cboCategory, lblFrom, dtFrom, lblTo, dtTo, lblSearch, txtSearch, lblSort, cboSort, btnApply, btnClear, picLogo });

            // EVENTS LIST
            lvEvents = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true };
            lvEvents.Columns.Add("Date/Time", 140);
            lvEvents.Columns.Add("Title", 280);
            lvEvents.Columns.Add("Category", 120);
            lvEvents.Columns.Add("Location", 180);
            lvEvents.Columns.Add("Description", 360);

            lvEvents.DoubleClick += (s, e) =>
            {
                if (lvEvents.SelectedItems.Count == 0) return;
                var tag = lvEvents.SelectedItems[0].Tag as Event;
                if (tag != null)
                {
                    EventStore.LastViewed.Push(tag);
                    MessageBox.Show(tag.ToString(), "Event details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            // BOTTOM PANEL: Count + Recommendations + Announcements
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 360, BackColor = Color.White };

            lblCount = new Label { Text = "0 event(s)", AutoSize = true, Location = new Point(20, 12) };

            // Recommendations
            lblReco = new Label
            {
                Text = "Recommended for you",
                AutoSize = true,
                Location = new Point(20, 42),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            lvRecommendations = new ListView
            {
                Location = new Point(20, 70),
                Size = new Size(920, 110),
                View = View.Details,
                FullRowSelect = true
            };
            lvRecommendations.Columns.Add("Date/Time", 140);
            lvRecommendations.Columns.Add("Title", 280);
            lvRecommendations.Columns.Add("Category", 120);
            lvRecommendations.Columns.Add("Location", 180);
            lvRecommendations.Columns.Add("Reason", 200);

            // ---- NEW: Announcements (not filtered) ----
            lblAnn = new Label
            {
                Text = "Announcements",
                AutoSize = true,
                Location = new Point(20, 190),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            lvAnnouncements = new ListView
            {
                Location = new Point(20, 215),
                Size = new Size(920, 120),
                View = View.Details,
                FullRowSelect = true
            };
            lvAnnouncements.Columns.Add("Date", 120);
            lvAnnouncements.Columns.Add("Title", 320);
            lvAnnouncements.Columns.Add("Department", 160);
            lvAnnouncements.Columns.Add("Message", 300);

            bottom.Controls.Add(lblCount);
            bottom.Controls.Add(lblReco);
            bottom.Controls.Add(lvRecommendations);
            bottom.Controls.Add(lblAnn);             // NEW
            bottom.Controls.Add(lvAnnouncements);    // NEW

            Controls.Add(lvEvents);
            Controls.Add(bottom);
            Controls.Add(pnlFilters);
        }

        private void LoadFilters()
        {
            var items = new List<object> { "All" };
            items.AddRange(EventStore.Categories.Select(c => (object)c));
            cboCategory.Items.Clear();
            cboCategory.Items.AddRange(items.ToArray());
            cboCategory.SelectedIndex = 0;

            dtFrom.Value = new DateTime(DateTime.Now.Year, 1, 1);
            dtTo.Value = new DateTime(DateTime.Now.Year, 12, 31);
        }

        private void ApplyFilters()
        {
            EventCategory? cat = null;
            if (cboCategory.SelectedIndex > 0) cat = (EventCategory)cboCategory.SelectedItem;

            DateTime? from = dtFrom.Value.Date;
            DateTime? to = dtTo.Value.Date;

            var found = EventStore.Find(from, to, cat, txtSearch.Text);
            var sorted = EventStore.Sort(found, cboSort.SelectedItem?.ToString());

            lvEvents.BeginUpdate();
            lvEvents.Items.Clear();
            foreach (var e in sorted)
            {
                var item = new ListViewItem(e.StartDate.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(e.Title);
                item.SubItems.Add(e.Category.ToString());
                item.SubItems.Add(e.Location);
                item.SubItems.Add(e.Description);
                item.Tag = e;
                lvEvents.Items.Add(item);
            }
            lvEvents.EndUpdate();

            lblCount.Text = $"{lvEvents.Items.Count} event(s)";
        }

        private void RecordSearch()
        {
            EventCategory? cat = null;
            if (cboCategory.SelectedIndex > 0)
                cat = (EventCategory)cboCategory.SelectedItem;

            DateTime? date = dtFrom.Value.Date;
            EventStore.Tracker.Record(cat, date, txtSearch.Text);
        }

        private void LoadRecommendations()
        {
            var recos = EventStore.Recommend(5).ToList();

            lvRecommendations.BeginUpdate();
            lvRecommendations.Items.Clear();

            if (recos.Count == 0)
            {
                lvRecommendations.Items.Add(new ListViewItem(new[] { "", "No recommendations yet — try searching.", "", "", "" }));
            }
            else
            {
                foreach (var e in recos)
                {
                    var item = new ListViewItem(e.StartDate.ToString("yyyy-MM-dd HH:mm"));
                    item.SubItems.Add(e.Title);
                    item.SubItems.Add(e.Category.ToString());
                    item.SubItems.Add(e.Location);
                    item.SubItems.Add("Based on your recent searches");
                    item.Tag = e;
                    lvRecommendations.Items.Add(item);
                }
            }

            lvRecommendations.EndUpdate();
        }

        // ---- NEW: Load a simple, unfiltered announcement list ----
        private void LoadAnnouncements()
        {
            var data = GetAnnouncements();

            lvAnnouncements.BeginUpdate();
            lvAnnouncements.Items.Clear();

            foreach (var a in data)
            {
                var item = new ListViewItem(a.Date.ToString("yyyy-MM-dd"));
                item.SubItems.Add(a.Title);
                item.SubItems.Add(a.Department);
                item.SubItems.Add(a.Message);
                lvAnnouncements.Items.Add(item);
            }

            if (lvAnnouncements.Items.Count == 0)
            {
                lvAnnouncements.Items.Add(new ListViewItem(new[] { "", "No announcements available.", "", "" }));
            }

            lvAnnouncements.EndUpdate();
        }

        // Demo announcements. Replace with your store later if needed.
        private List<Announcement> GetAnnouncements()
        {
            return new List<Announcement>
            {
                new Announcement
                {
                    Date = DateTime.Today,
                    Title = "Planned Water Interruption — Ward 34",
                    Department = "Water & Sanitation",
                    Message = "Maintenance 09:00–14:00. Please store sufficient water."
                },
                new Announcement
                {
                    Date = DateTime.Today.AddDays(1),
                    Title = "Roadworks on Kingsway Rd",
                    Department = "Roads & Transport",
                    Message = "Expect delays; one lane closed 07:00–17:00."
                },
                new Announcement
                {
                    Date = DateTime.Today.AddDays(2),
                    Title = "Refuse Collection Schedule Update",
                    Department = "Waste Management",
                    Message = "Friday pickups moved to Saturday due to public holiday."
                }
            };
        }

        // Simple record type for announcements (kept local to this form)
        private class Announcement
        {
            public DateTime Date { get; set; }
            public string Title { get; set; }
            public string Department { get; set; }
            public string Message { get; set; }
        }
    }
}
