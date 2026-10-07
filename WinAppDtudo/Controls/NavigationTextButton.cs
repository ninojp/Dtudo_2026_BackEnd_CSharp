using WinAppDtudo.Services;

namespace WinAppDtudo.Controls;

public sealed class NavigationTextButton : Button
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Enabled)
            return;

        // O renderer Flat do WinForms ignora ForeColor para o texto desabilitado.
        using var background = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(background, ClientRectangle);
        var borderSize = FlatAppearance.BorderSize;
        if (borderSize > 0)
        {
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
                FlatAppearance.BorderColor, borderSize, ButtonBorderStyle.Solid,
                FlatAppearance.BorderColor, borderSize, ButtonBorderStyle.Solid,
                FlatAppearance.BorderColor, borderSize, ButtonBorderStyle.Solid,
                FlatAppearance.BorderColor, borderSize, ButtonBorderStyle.Solid);
        }

        var textBounds = Rectangle.FromLTRB(
            Padding.Left + borderSize,
            Padding.Top + borderSize,
            ClientSize.Width - Padding.Right - borderSize,
            ClientSize.Height - Padding.Bottom - borderSize);
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds,
            DarkModeColors.NavigationDisabledTextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }
}
