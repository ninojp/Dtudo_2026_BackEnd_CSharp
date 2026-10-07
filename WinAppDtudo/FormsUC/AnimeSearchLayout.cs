namespace WinAppDtudo.FormsUC;

internal static class AnimeSearchLayout
{
    public static void Build(
        UserControl owner,
        Label searchLabel,
        TextBox searchTextBox,
        Button localSearchButton,
        Button externalSearchButton,
        Label status,
        FlowLayoutPanel cards,
        Button previousButton,
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
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerLayout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Black,
            Padding = new Padding(24, 20, 24, 12),
            TabIndex = 0
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (var row = 0; row < headerLayout.RowCount; row++)
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var sourceButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.None,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 16),
            TabIndex = 1
        };

        ConfigureActionButton(localSearchButton);
        ConfigureActionButton(externalSearchButton);
        var sourceButtonSize = new Size(
            Math.Max(260, Math.Max(localSearchButton.MinimumSize.Width, externalSearchButton.MinimumSize.Width)),
            Math.Max(56, Math.Max(localSearchButton.MinimumSize.Height, externalSearchButton.MinimumSize.Height)));
        localSearchButton.MinimumSize = sourceButtonSize;
        externalSearchButton.MinimumSize = sourceButtonSize;
        localSearchButton.Margin = Padding.Empty;
        externalSearchButton.Margin = new Padding(100, 0, 0, 0);
        localSearchButton.TabIndex = 0;
        externalSearchButton.TabIndex = 1;
        sourceButtons.Controls.Add(localSearchButton);
        sourceButtons.Controls.Add(externalSearchButton);

        searchLabel.AutoSize = true;
        searchLabel.Dock = DockStyle.Top;
        searchLabel.TextAlign = ContentAlignment.MiddleCenter;
        searchLabel.Margin = new Padding(0, 0, 0, 4);

        searchTextBox.Dock = DockStyle.Fill;
        // Acrescenta 150 px por lado para reduzir a largura anterior em 300 px.
        searchTextBox.Margin = new Padding(208, 0, 208, 0);
        searchTextBox.MinimumSize = new Size(0, searchTextBox.PreferredHeight);
        searchTextBox.TabIndex = 0;

        status.AutoSize = false;
        status.AutoEllipsis = true;
        status.Dock = DockStyle.Fill;
        status.TextAlign = ContentAlignment.MiddleCenter;
        status.MinimumSize = new Size(0, status.Font.Height + 4);
        status.Margin = new Padding(12, 0, 12, 0);

        headerLayout.Controls.Add(sourceButtons, 0, 0);
        headerLayout.Controls.Add(searchLabel, 0, 1);
        headerLayout.Controls.Add(searchTextBox, 0, 2);

        cards.Dock = DockStyle.Fill;
        cards.AutoScroll = true;
        cards.WrapContents = true;
        cards.FlowDirection = FlowDirection.LeftToRight;
        cards.BackColor = Color.Black;
        cards.Padding = new Padding(24, 12, 24, 12);
        cards.AutoScrollMargin = new Size(12, 12);
        cards.TabIndex = 1;

        var paginationLayout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Black,
            Padding = new Padding(24, 4, 24, 4),
            TabIndex = 2
        };
        paginationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paginationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        paginationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paginationLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        ConfigureNavigationButton(previousButton);
        ConfigureNavigationButton(nextButton);

        paginationLayout.Controls.Add(previousButton, 0, 0);
        paginationLayout.Controls.Add(status, 1, 0);
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

    private static void ConfigureNavigationButton(Button button)
    {
        button.AutoSize = false;
        button.MinimumSize = new Size(104, 30);
        button.Size = button.MinimumSize;
        button.Padding = new Padding(6, 2, 6, 2);
        button.Margin = Padding.Empty;
        button.Dock = DockStyle.Fill;
        button.Enabled = false;
    }
}
