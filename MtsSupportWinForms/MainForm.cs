using System;
using System.Drawing;
using System.Windows.Forms;

namespace MtsSupportWinForms
{
    public class MainForm : Form
    {
        private readonly UserAccount _user;
        private readonly FlowLayoutPanel _statsPanel = new FlowLayoutPanel();
        private readonly FlowLayoutPanel _tilesPanel = new FlowLayoutPanel();
        private readonly DateTimePicker _dtStatsFrom = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short };
        private readonly DateTimePicker _dtStatsTo = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short };
        public bool ReturnToLogin { get; private set; }

        public MainForm(UserAccount user)
        {
            _user = user;
            Theme.StyleForm(this);
            Text = "Главное окно";
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;

            var sidebar = new Panel { Dock = DockStyle.Left, Width = 250, BackColor = Theme.Sidebar, Padding = new Padding(18) };
            var brand = new Label
            {
                Text = "MTS Support\nDesktop",
                Dock = DockStyle.Top,
                Height = 86,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White
            };
            var btnLogout = Theme.CreatePrimaryButton("Выход", 210);
            btnLogout.Dock = DockStyle.Bottom;
            btnLogout.Margin = new Padding(0, 0, 0, 8);
            btnLogout.Click += delegate
            {
                ReturnToLogin = true;
                Close();
            };
            sidebar.Controls.Add(btnLogout);
            sidebar.Controls.Add(brand);

            var top = new Panel { Dock = DockStyle.Top, Height = 110, Padding = new Padding(24, 16, 24, 14), BackColor = Theme.Surface };
            var title = new Label
            {
                Text = GetMainModuleTitle(),
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Theme.Text
            };
            var subtitle = new Label
            {
                Text = GetAccessSubtitle(),
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Theme.Muted
            };
            _dtStatsFrom.Value = DateTime.Today.AddMonths(-1);
            _dtStatsTo.Value = DateTime.Today;
            _dtStatsFrom.ValueChanged += delegate { FillStats(); };
            _dtStatsTo.ValueChanged += delegate { FillStats(); };
            var statsPeriod = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 30, WrapContents = false };
            statsPeriod.Controls.Add(new Label { Text = "Статистика с", AutoSize = true, Padding = new Padding(0, 6, 0, 0), ForeColor = Theme.Muted });
            statsPeriod.Controls.Add(_dtStatsFrom);
            statsPeriod.Controls.Add(new Label { Text = "по", AutoSize = true, Padding = new Padding(4, 6, 0, 0), ForeColor = Theme.Muted });
            statsPeriod.Controls.Add(_dtStatsTo);
            top.Controls.Add(subtitle);
            top.Controls.Add(title);
            top.Controls.Add(statsPeriod);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22) };
            _statsPanel.Dock = DockStyle.Top;
            _statsPanel.Height = 138;
            _statsPanel.WrapContents = true;
            _tilesPanel.Dock = DockStyle.Fill;
            _tilesPanel.WrapContents = true;
            _tilesPanel.AutoScroll = true;
            _tilesPanel.Padding = new Padding(0, 14, 0, 0);

            body.Controls.Add(_tilesPanel);
            body.Controls.Add(_statsPanel);

            Controls.Add(body);
            Controls.Add(top);
            Controls.Add(sidebar);
            Load += delegate { FillStats(); FillTiles(); };
        }

        private void FillStats()
        {
            _statsPanel.Controls.Clear();
            if (_dtStatsFrom.Value.Date > _dtStatsTo.Value.Date) return;
            AddStatCard("Клиенты", SafeCount("SELECT COUNT(*) FROM Client").ToString(), Theme.Primary);
            AddStatCard("Обращения", SafeCount("SELECT COUNT(*) FROM Request WHERE date_request >= @dateFrom AND date_request < DATEADD(DAY,1,@dateTo)", _dtStatsFrom.Value.Date, _dtStatsTo.Value.Date).ToString(), Theme.Success);
            AddStatCard("Открытые заявки", SafeCount("SELECT COUNT(*) FROM Request r INNER JOIN Status s ON s.status_id=r.status_id WHERE s.title_status <> N'Закрыто' AND r.date_request >= @dateFrom AND r.date_request < DATEADD(DAY,1,@dateTo)", _dtStatsFrom.Value.Date, _dtStatsTo.Value.Date).ToString(), Theme.Warning);
            if (_user.Role != UserRole.OperatorLine1)
            {
                AddStatCard("Решения", SafeCount("SELECT COUNT(*) FROM Solution WHERE date_create >= @dateFrom AND date_create < DATEADD(DAY,1,@dateTo)", _dtStatsFrom.Value.Date, _dtStatsTo.Value.Date).ToString(), Color.FromArgb(56, 96, 178));
            }
            if (_user.Role == UserRole.Administrator)
            {
                AddStatCard("Сотрудники", SafeCount("SELECT COUNT(*) FROM Employee").ToString(), Color.FromArgb(126, 87, 194));
            }
        }

        private void FillTiles()
        {
            _tilesPanel.Controls.Clear();
            AddTile("Клиенты", "Поиск, карточка клиента, изменение и удаление записей.", delegate { OpenModule("Клиенты", new ClientsForm(_user)); }, true);
            AddTile("Обращения", "Регистрация заявок, назначение сотрудника, изменение статуса и контроль сроков.", delegate { OpenModule("Обращения", new RequestsForm(_user)); }, true);
            AddTile("Оборудование", "Справочник устройств клиентов и связь с моделями оборудования.", delegate { OpenModule("Оборудование", new EquipmentForm(_user)); }, _user.Role != UserRole.OperatorLine1);
            AddTile("Сотрудники", "Администрирование сотрудников и должностей.", delegate { OpenModule("Сотрудники", new EmployeesForm(_user)); }, _user.Role == UserRole.Administrator);
            AddTile("Решения", "База знаний по устранению типовых инцидентов.", delegate { OpenModule("Решения", new SolutionsForm(_user)); }, _user.Role != UserRole.OperatorLine1);
            AddTile("Отчеты", "Аналитика по статусам, нагрузке, срокам и решениям.", delegate { OpenModule("Отчеты", new ReportsForm()); }, _user.Role == UserRole.Administrator);
            AddTile("Журналирование", "Просмотр действий пользователей и операций изменения данных.", delegate { OpenModule("Журнал", new ActivityLogForm()); }, _user.Role == UserRole.Administrator);
        }

        private void AddTile(string title, string description, Action action, bool visible)
        {
            if (!visible) return;
            var card = Theme.CreateCard();
            card.Width = 240;
            card.Height = 172;
            card.Margin = new Padding(8);
            card.Cursor = Cursors.Hand;

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Theme.Text
            };
            var descLabel = new Label
            {
                Text = description,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Theme.Muted
            };
            var openButton = Theme.CreatePrimaryButton("Открыть раздел", 140);
            openButton.Dock = DockStyle.Bottom;
            openButton.Click += delegate { action(); };

            card.Click += delegate { action(); };
            titleLabel.Click += delegate { action(); };
            descLabel.Click += delegate { action(); };

            card.Controls.Add(openButton);
            card.Controls.Add(descLabel);
            card.Controls.Add(titleLabel);
            _tilesPanel.Controls.Add(card);
        }

        private void AddStatCard(string title, string value, Color accent)
        {
            var card = Theme.CreateStatCard(title, value, accent);
            card.Width = 180;
            card.Margin = new Padding(8);
            _statsPanel.Controls.Add(card);
        }

        private void OpenModule(string moduleName, Form form)
        {
            LogService.Log("Открытие модуля", moduleName + " | " + _user.Email);
            using (form)
            {
                form.ShowDialog(this);
            }
            FillStats();
        }


        private string GetMainModuleTitle()
        {
            if (_user.Role == UserRole.Administrator)
            {
                return "Главный модуль администратора";
            }

            if (_user.Role == UserRole.OperatorLine1)
            {
                return "Главный модуль оператора 1 линии";
            }

            return "Оператор 2 линии";
        }

        private string GetAccessSubtitle()
        {
            if (_user.Role == UserRole.Administrator)
            {
                return "Доступ к клиентам, обращениям, оборудованию, сотрудникам, решениям, отчетам и журналированию.";
            }

            if (_user.Role == UserRole.OperatorLine1)
            {
                return "Доступ к клиентам и обращениям.";
            }

            return "Доступ к клиентам, обращениям, оборудованию и решениям.";
        }
        private int SafeCount(string sql, params object[] dateArgs)
        {
            try
            {
                if (dateArgs.Length == 2)
                {
                    return Db.Count(sql, new System.Data.SqlClient.SqlParameter("@dateFrom", dateArgs[0]), new System.Data.SqlClient.SqlParameter("@dateTo", dateArgs[1]));
                }
                return Db.Count(sql);
            }
            catch
            {
                return 0;
            }
        }
    }
}
