using System;
using System.IO;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices; // cue banner placeholder

namespace MunicipalServicesApp
{
    /// <summary>
    /// ReportIssueForm: lets residents capture a municipal issue
    /// with location, category, description and optional attachments.
    /// Uses a custom SimpleLinkedList<string> to satisfy the data-structure requirement.
    /// </summary>
    public class ReportIssueForm : Form
    {
        // --- Top section (title + helper text) ---
        private Label lblHeader;
        private Label lblSubtitle;

        // --- Details group (location, category, description) ---
        private GroupBox grpDetails;
        private Label lblLocation;
        private TextBox txtLocation;
        private Label lblCategory;
        private ComboBox cboCategory;
        private Label lblDescription;
        private RichTextBox rtbDescription;
        private Label lblDescCount; // live character counter

        // --- Attachments group (add/remove, listbox) ---
        private GroupBox grpAttachments;
        private Button btnAttach;
        private Button btnRemoveAttachment;
        private ListBox lstAttachments;

        // --- Engagement nudges + actions ---
        private ProgressBar prgEngagement;
        private Label lblEngagementStatus;
        private Button btnSubmit;
        private Button btnBack;

        private ErrorProvider errorProvider;

        // Store attachment full paths in a CUSTOM linked list (no List<T>)
        private readonly SimpleLinkedList<string> _attachments = new SimpleLinkedList<string>();

        // Validation thresholds
        private const int MinDescriptionLength = 20;
        private const int MaxDescriptionLength = 500; // soft cap

        // Track unsaved changes for close-confirmation
        private bool _isDirty = false;

        // Win32: set a placeholder (cue banner) on TextBox for .NET Framework
        private const int EM_SETCUEBANNER = 0x1501;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        /// <summary>Apply a native cue banner placeholder to a TextBox.</summary>
        private static void SetCueBanner(TextBox textBox, string placeholder, bool showWhenFocused = false)
        {
            if (textBox == null || textBox.IsDisposed) return;

            Action apply = () =>
            {
                // wParam: 1 = show when focused, 0 = hide when focused
                SendMessage(textBox.Handle, EM_SETCUEBANNER, (IntPtr)(showWhenFocused ? 1 : 0), placeholder);
            };

            if (textBox.IsHandleCreated) apply();
            else textBox.HandleCreated += (s, e) => apply();
        }

        /// <summary>Small helper to validate file extensions (avoids HashSet).</summary>
        private static bool IsAllowedExtension(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return false;
            string e = ext.ToLowerInvariant();
            return e == ".jpg" || e == ".jpeg" || e == ".png" || e == ".pdf";
        }

        public ReportIssueForm()
        {
            InitializeComponent();
            PopulateCategories();
            UpdateDescCounter();
            UpdateEngagementProgress();
            UpdateSubmitEnabled();
        }

        /// <summary>Build the UI and wire core events.</summary>
        private void InitializeComponent()
        {
            // Window
            Text = "Report an Issue";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(840, 700);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            // Theme colors
            Color primary = Color.FromArgb(10, 110, 189);  // #0A6EBD
            Color accent = Color.FromArgb(52, 168, 83);   // #34A853

            // Inline validation provider (red icon + tooltip)
            errorProvider = new ErrorProvider
            {
                BlinkStyle = ErrorBlinkStyle.NeverBlink,
                ContainerControl = this
            };

            // Header
            lblHeader = new Label
            {
                Text = "Report an Issue",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                ForeColor = primary,
                Location = new Point(28, 24),
                TabIndex = 0
            };

            lblSubtitle = new Label
            {
                Text = "Provide details below and attach images/documents if available.",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Location = new Point(32, 66),
                TabIndex = 1
            };

            // === Details group ===
            grpDetails = new GroupBox
            {
                Text = "Issue Details",
                Location = new Point(28, 100),
                Size = new Size(770, 320),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TabIndex = 2
            };

            // Location
            lblLocation = new Label { Text = "Location:", AutoSize = true, Location = new Point(18, 34), TabIndex = 0 };
            txtLocation = new TextBox
            {
                Name = "txtLocation",
                Location = new Point(120, 30),
                Size = new Size(620, 27),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TabIndex = 1
            };
            txtLocation.TextChanged += AnyFieldChanged;
            SetCueBanner(txtLocation, "e.g., 123 Florence Nzama St, Durban CBD");

            // Category
            lblCategory = new Label { Text = "Category:", AutoSize = true, Location = new Point(18, 74), TabIndex = 2 };
            cboCategory = new ComboBox
            {
                Name = "cboCategory",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(120, 70),
                Size = new Size(300, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                TabIndex = 3
            };
            cboCategory.SelectedIndexChanged += AnyFieldChanged;

            // Description
            lblDescription = new Label { Text = "Description:", AutoSize = true, Location = new Point(18, 114), TabIndex = 4 };
            rtbDescription = new RichTextBox
            {
                Name = "rtbDescription",
                Location = new Point(120, 110),
                Size = new Size(620, 170),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TabIndex = 5
            };
            rtbDescription.TextChanged += DescriptionChanged;

            lblDescCount = new Label
            {
                Text = "0 / 500",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Location = new Point(120, 284),
                TabIndex = 6
            };

            grpDetails.Controls.Add(lblLocation);
            grpDetails.Controls.Add(txtLocation);
            grpDetails.Controls.Add(lblCategory);
            grpDetails.Controls.Add(cboCategory);
            grpDetails.Controls.Add(lblDescription);
            grpDetails.Controls.Add(rtbDescription);
            grpDetails.Controls.Add(lblDescCount);

            // === Attachments group ===
            grpAttachments = new GroupBox
            {
                Text = "Attachments (optional: JPG/PNG/PDF)",
                Location = new Point(28, 430),
                Size = new Size(770, 150),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TabIndex = 3
            };

            btnAttach = new Button
            {
                Name = "btnAttach",
                Text = "Add Images / Documents…",
                Size = new Size(220, 36),
                Location = new Point(18, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                TabIndex = 0
            };
            btnAttach.Click += BtnAttach_Click;

            btnRemoveAttachment = new Button
            {
                Name = "btnRemoveAttachment",
                Text = "Remove Selected",
                Size = new Size(160, 36),
                Location = new Point(18, 76),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                TabIndex = 1
            };
            btnRemoveAttachment.Click += BtnRemoveAttachment_Click;

            // Quick tips on buttons
            var tip = new ToolTip();
            tip.SetToolTip(btnAttach, "Attach JPG/PNG images or PDF documents");
            tip.SetToolTip(btnRemoveAttachment, "Remove the selected file(s) from the list");

            lstAttachments = new ListBox
            {
                Name = "lstAttachments",
                Location = new Point(260, 28),
                Size = new Size(480, 98),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                SelectionMode = SelectionMode.MultiExtended,
                TabIndex = 2
            };

            grpAttachments.Controls.Add(btnAttach);
            grpAttachments.Controls.Add(btnRemoveAttachment);
            grpAttachments.Controls.Add(lstAttachments);

            // Engagement nudges
            prgEngagement = new ProgressBar
            {
                Name = "prgEngagement",
                Location = new Point(28, 590),
                Size = new Size(500, 22),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                TabIndex = 4
            };

            lblEngagementStatus = new Label
            {
                Text = "Let’s get started.",
                AutoSize = true,
                Location = new Point(536, 590),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                ForeColor = accent,
                TabIndex = 5
            };

            // Action buttons
            btnSubmit = new Button
            {
                Name = "btnSubmit",
                Text = "Submit",
                Size = new Size(120, 40),
                Location = new Point(678, 625),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                TabIndex = 7
            };
            btnSubmit.Click += BtnSubmit_Click;

            btnBack = new Button
            {
                Name = "btnBack",
                Text = "Back to Main Menu",
                Size = new Size(180, 40),
                Location = new Point(28, 625),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                TabIndex = 6
            };
            btnBack.Click += delegate { this.Close(); };

            // Form conveniences
            this.AcceptButton = btnSubmit;
            this.CancelButton = btnBack;

            // Warn before closing if there are unsaved edits
            this.FormClosing += ReportIssueForm_FormClosing;

            // Add to form
            Controls.Add(lblHeader);
            Controls.Add(lblSubtitle);
            Controls.Add(grpDetails);
            Controls.Add(grpAttachments);
            Controls.Add(prgEngagement);
            Controls.Add(lblEngagementStatus);
            Controls.Add(btnSubmit);
            Controls.Add(btnBack);
        }

        /// <summary>Populate the static category list for now.</summary>
        private void PopulateCategories()
        {
            cboCategory.Items.AddRange(new object[]
            {
                "Sanitation",
                "Roads & Stormwater",
                "Water & Utilities",
                "Electricity",
                "Parks & Recreation",
                "Safety & Security",
                "Housing",
                "Other"
            });
            cboCategory.SelectedIndex = -1;
        }

        // ===== Events & Handlers =====

        /// <summary>Keeps description within soft max and updates counters.</summary>
        private void DescriptionChanged(object sender, EventArgs e)
        {
            // Trim if user pastes a very long text
            if (rtbDescription.TextLength > MaxDescriptionLength)
            {
                int selStart = rtbDescription.SelectionStart;
                rtbDescription.Text = rtbDescription.Text.Substring(0, MaxDescriptionLength);
                rtbDescription.SelectionStart = Math.Min(selStart, rtbDescription.TextLength);
            }

            AnyFieldChanged(sender, e);
            UpdateDescCounter();
        }

        /// <summary>Refresh the live character count label.</summary>
        private void UpdateDescCounter()
        {
            int len = (rtbDescription.Text ?? string.Empty).Trim().Length;
            lblDescCount.Text = len + " / " + MaxDescriptionLength;
            lblDescCount.ForeColor = (len < MinDescriptionLength) ? Color.IndianRed : Color.DimGray;
        }

        /// <summary>Open file picker and add selected attachments.</summary>
        private void BtnAttach_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Attach images or documents";
                ofd.Filter = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|PDF files (*.pdf)|*.pdf|All files (*.*)|*.*";
                ofd.Multiselect = true;

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    foreach (string path in ofd.FileNames)
                    {
                        AddAttachment(path);
                    }
                    UpdateEngagementProgress();
                    UpdateSubmitEnabled();
                }
            }
        }

        /// <summary>Remove selected items from the custom attachments list.</summary>
        private void BtnRemoveAttachment_Click(object sender, EventArgs e)
        {
            if (lstAttachments.SelectedItems.Count == 0) return;

            // Remove by matching the display name (file name)
            for (int i = lstAttachments.SelectedItems.Count - 1; i >= 0; i--)
            {
                string display = lstAttachments.SelectedItems[i].ToString();
                _attachments.RemoveWhere(p => Path.GetFileName(p) == display);
            }

            // Refresh UI list from custom list
            lstAttachments.Items.Clear();
            _attachments.ForEach(p => lstAttachments.Items.Add(Path.GetFileName(p)));

            _isDirty = true;
            UpdateEngagementProgress();
            UpdateSubmitEnabled();
        }

        /// <summary>Add a single attachment if valid and not already present.</summary>
        private void AddAttachment(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            string ext = Path.GetExtension(path);
            if (!File.Exists(path)) return;
            if (!IsAllowedExtension(ext)) return;

            if (!_attachments.Contains(path))
            {
                _attachments.Add(path);
                lstAttachments.Items.Add(Path.GetFileName(path));
                _isDirty = true;
            }
        }

        /// <summary>Validate inputs, create IssueReport, save, and show ref number.</summary>
        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            string summary;
            Control firstInvalid;

            bool ok = ValidateForm(out summary, out firstInvalid);

            if (!ok)
            {
                MessageBox.Show(summary, "Please fix the following", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (firstInvalid != null) firstInvalid.Focus();
                return;
            }

            // Build the report object from UI
            IssueReport report = new IssueReport
            {
                Reference = ReferenceGenerator.Next(),
                Location = txtLocation.Text.Trim(),
                Category = cboCategory.SelectedItem != null ? cboCategory.SelectedItem.ToString() : "",
                Description = (rtbDescription.Text ?? string.Empty).Trim(),
                CreatedAt = DateTime.Now,
                Status = "Received"
            };

            // Copy attachments into a fresh custom list (immutability of input)
            var atts = new SimpleLinkedList<string>();
            _attachments.ForEach(p => atts.Add(p));
            report.Attachments = atts;

            // Save to in-memory repository (also custom structure inside)
            IssueRepository.Add(report);

            int total = IssueRepository.Count;
            DialogResult dr = MessageBox.Show(
                "Issue submitted successfully.\n\n" +
                "Reference: " + report.Reference + "\n" +
                "Category: " + report.Category + "\n" +
                "Submitted: " + report.CreatedAt.ToString("yyyy-MM-dd HH:mm") + "\n" +
                "Total reports stored this session: " + total + "\n\n" +
                "Would you like to submit another issue?",
                "Submission successful",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (dr == DialogResult.Yes)
            {
                ClearForm();
            }
            else
            {
                _isDirty = false; // nothing unsaved
                this.Close();
            }
        }

        /// <summary>Full validation with inline errors + summary text.</summary>
        private bool ValidateForm(out string summary, out Control firstInvalid)
        {
            firstInvalid = null;
            errorProvider.Clear();

            bool ok = true;
            StringBuilder sb = new StringBuilder();

            // Location
            if (string.IsNullOrWhiteSpace(txtLocation.Text) || txtLocation.Text.Trim().Length < 3)
            {
                ok = false;
                if (firstInvalid == null) firstInvalid = txtLocation;
                errorProvider.SetError(txtLocation, "Please enter a valid location (min 3 characters).");
                sb.AppendLine("• Enter a valid Location (min 3 characters).");
            }

            // Category
            if (cboCategory.SelectedIndex < 0)
            {
                ok = false;
                if (firstInvalid == null) firstInvalid = cboCategory;
                errorProvider.SetError(cboCategory, "Please select a category.");
                sb.AppendLine("• Select a Category.");
            }

            // Description
            string desc = (rtbDescription.Text ?? string.Empty).Trim();
            if (desc.Length < MinDescriptionLength)
            {
                ok = false;
                if (firstInvalid == null) firstInvalid = rtbDescription;
                errorProvider.SetError(rtbDescription, "Please describe the issue (at least 20 characters).");
                sb.AppendLine("• Provide a Description (at least 20 characters).");
            }

            // Attachments (optional, but validate if present)
            bool attachmentError = false;
            string attachErrorMsg = null;

            _attachments.ForEach(path =>
            {
                if (attachmentError) return; // soft short-circuit

                string ext = Path.GetExtension(path);
                if (!File.Exists(path))
                {
                    attachmentError = true;
                    attachErrorMsg = "• One or more attachments no longer exist on disk.";
                    return;
                }
                if (!IsAllowedExtension(ext))
                {
                    attachmentError = true;
                    attachErrorMsg = "• Only JPG, JPEG, PNG, or PDF attachments are allowed.";
                }
            });

            if (attachmentError)
            {
                ok = false;
                if (firstInvalid == null) firstInvalid = lstAttachments;
                sb.AppendLine(attachErrorMsg);
            }

            summary = ok ? "All checks passed." : "Validation issues:\n\n" + sb.ToString();

            UpdateEngagementProgress();
            UpdateSubmitEnabled();
            return ok;
        }

        /// <summary>Lightweight readiness check (drives Submit.Enabled).</summary>
        private bool IsFormValid()
        {
            if (string.IsNullOrWhiteSpace(txtLocation.Text) || txtLocation.Text.Trim().Length < 3) return false;
            if (cboCategory.SelectedIndex < 0) return false;

            string desc = (rtbDescription.Text ?? string.Empty).Trim();
            if (desc.Length < MinDescriptionLength) return false;

            bool invalid = false;
            _attachments.ForEach(path =>
            {
                if (invalid) return;
                string ext = Path.GetExtension(path);
                if (!File.Exists(path)) { invalid = true; return; }
                if (!IsAllowedExtension(ext)) { invalid = true; }
            });

            return !invalid;
        }

        /// <summary>Enable/disable Submit based on silent validation.</summary>
        private void UpdateSubmitEnabled()
        {
            btnSubmit.Enabled = IsFormValid();
        }

        /// <summary>Marks dirty, updates engagement meter, and clears inline error.</summary>
        private void AnyFieldChanged(object sender, EventArgs e)
        {
            _isDirty = true;
            UpdateEngagementProgress();
            UpdateSubmitEnabled();
            if (sender is Control c) errorProvider.SetError(c, ""); // clear inline error while typing
        }

        /// <summary>Simple progress bar to nudge completion.</summary>
        private void UpdateEngagementProgress()
        {
            int milestones = 0;

            if (!string.IsNullOrWhiteSpace(txtLocation.Text)) milestones++;
            if (cboCategory.SelectedIndex >= 0) milestones++;
            if (!string.IsNullOrWhiteSpace(rtbDescription.Text) &&
                rtbDescription.Text.Trim().Length >= MinDescriptionLength) milestones++;
            if (_attachments.Count > 0) milestones++;

            int percent = milestones * 25;
            if (percent < prgEngagement.Minimum) percent = prgEngagement.Minimum;
            if (percent > prgEngagement.Maximum) percent = prgEngagement.Maximum;
            prgEngagement.Value = percent;

            string status;
            if (percent == 0)
                status = "Let’s get started.";
            else if (percent == 25)
                status = "Great start — keep going.";
            else if (percent == 50)
                status = "Halfway there!";
            else if (percent == 75)
                status = "Almost done — you can submit.";
            else if (percent == 100)
                status = "Ready to submit. Thank you!";
            else
                status = "Keep going.";

            lblEngagementStatus.Text = status;
        }

        /// <summary>Warn if closing with unsaved changes.</summary>
        private void ReportIssueForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isDirty) return;

            DialogResult dr = MessageBox.Show(
                "You have unsaved information. Do you really want to close?",
                "Discard changes?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        /// <summary>Clear the form to capture a new issue.</summary>
        private void ClearForm()
        {
            errorProvider.Clear();
            txtLocation.Text = "";
            cboCategory.SelectedIndex = -1;
            rtbDescription.Clear();
            _attachments.Clear();
            lstAttachments.Items.Clear();
            UpdateDescCounter();
            UpdateEngagementProgress();
            UpdateSubmitEnabled();
            _isDirty = false;
            txtLocation.Focus();
        }
    }
}
// Reference: Hart, T.G.B., et al. (2020) ‘Innovation for development in South Africa: Experiences with basic service technologies in distressed municipalities’, Forum for Development Studies, 47(1), pp. 2–347. Available at: https://strathprints.strath.ac.uk/73688/ (Accessed: 10 September 2025).
