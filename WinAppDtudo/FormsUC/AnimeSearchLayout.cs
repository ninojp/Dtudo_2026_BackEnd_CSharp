namespace WinAppDtudo.FormsUC;

internal static class AnimeSearchLayout
{
    public static void Build(
        UserControl owner,
        Label title,
        Label searchLabel,
        TextBox searchTextBox,
        Button searchButton,
        Label status,
        FlowLayoutPanel cards,
        Button previousButton,
        Label pageLabel,
        Button nextButton)
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Black,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerLayout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Black,
            Padding = new Padding(24, 20, 24, 12)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (var row = 0; row < headerLayout.RowCount; row++)
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        title.AutoSize = true;
        title.Dock = DockStyle.Top;
        title.Margin = new Padding(0, 0, 0, 12);

        searchLabel.AutoSize = true;
        searchLabel.Dock = DockStyle.Top;
        searchLabel.Margin = new Padding(0, 0, 0, 4);

        var searchLayout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        searchLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        searchTextBox.Dock = DockStyle.Fill;
        searchTextBox.Margin = new Padding(0, 0, 12, 0);
        searchTextBox.MinimumSize = new Size(0, searchTextBox.PreferredHeight);

        ConfigureActionButton(searchButton);
        searchButton.Margin = Padding.Empty;
        searchButton.Dock = DockStyle.Fill;

        status.AutoSize = false;
        status.AutoEllipsis = true;
        status.Dock = DockStyle.Fill;
        status.MinimumSize = new Size(0, status.Font.Height + 12);
        status.Margin = new Padding(0, 12, 0, 0);

        searchLayout.Controls.Add(searchTextBox, 0, 0);
        searchLayout.Controls.Add(searchButton, 1, 0);
        headerLayout.Controls.Add(title, 0, 0);
        headerLayout.Controls.Add(searchLabel, 0, 1);
        headerLayout.Controls.Add(searchLayout, 0, 2);
        headerLayout.Controls.Add(status, 0, 3);

        cards.Dock = DockStyle.Fill;
        cards.AutoScroll = true;
        cards.WrapContents = true;
        cards.FlowDirection = FlowDirection.LeftToRight;
        cards.BackColor = Color.Black;
        cards.Padding = new Padding(24, 12, 24, 12);
        cards.AutoScrollMargin = new Size(12, 12);

        var paginationLayout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Black,
            Padding = new Padding(24, 8, 24, 12)
        };
        paginationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paginationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        paginationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paginationLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        ConfigureActionButton(previousButton);
        ConfigureActionButton(nextButton);
        previousButton.Enabled = false;
        nextButton.Enabled = false;
        previousButton.Dock = DockStyle.Fill;
        nextButton.Dock = DockStyle.Fill;
        previousButton.Margin = Padding.Empty;
        nextButton.Margin = Padding.Empty;

        pageLabel.AutoEllipsis = true;
        pageLabel.AutoSize = false;
        pageLabel.Dock = DockStyle.Fill;
        pageLabel.TextAlign = ContentAlignment.MiddleCenter;
        pageLabel.Margin = new Padding(12, 0, 12, 0);

        paginationLayout.Controls.Add(previousButton, 0, 0);
        paginationLayout.Controls.Add(pageLabel, 1, 0);
        paginationLayout.Controls.Add(nextButton, 2, 0);

        mainLayout.Controls.Add(headerLayout, 0, 0);
        mainLayout.Controls.Add(cards, 0, 1);
        mainLayout.Controls.Add(paginationLayout, 0, 2);
        owner.Controls.Add(mainLayout);
    }

    private static void ConfigureActionButton(Button button)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = new Size(
            TextRenderer.MeasureText(button.Text, button.Font).Width + 32,
            button.Font.Height + 20);
        button.Padding = new Padding(14, 8, 14, 8);
    }
}
