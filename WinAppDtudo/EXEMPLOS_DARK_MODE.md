# 📝 Exemplos de Código - Dark Mode

Aqui estão exemplos de como usar o Dark Mode em diferentes cenários.

## 📌 Exemplo 1: Como Criar um Novo Formulário com Dark Mode

```csharp
using WinAppDtudo.Helpers;  // Importante!

namespace WinAppDtudo.Forms
{
    public partial class Frm_MeuNovoFormulario : BaseFormDarkMode
    {
        public Frm_MeuNovoFormulario()
        {
            InitializeComponent();

            // 🎨 Dark Mode é aplicado AUTOMATICAMENTE pela classe base!
            // Nada mais a fazer - o tema está pronto!

            // Configure seus componentes normalmente:
            this.Text = "Meu Novo Formulário";
            this.Size = new System.Drawing.Size(800, 600);
        }
    }
}
```

---

## 📌 Exemplo 2: Como Migrar um Formulário Existente

### Versão 1: Usando Herança (Recomendado)

**Antes:**
```csharp
public partial class Frm_Existente : Form
{
    public Frm_Existente()
    {
        InitializeComponent();
    }
}
```

**Depois:**
```csharp
using WinAppDtudo.Helpers;

public partial class Frm_Existente : BaseFormDarkMode  // ← Mude aqui
{
    public Frm_Existente()
    {
        InitializeComponent();
        // Pronto! Tema aplicado automaticamente!
    }
}
```

### Versão 2: Chamando a Função

**Antes:**
```csharp
public partial class Frm_Existente : Form
{
    public Frm_Existente()
    {
        InitializeComponent();
    }
}
```

**Depois:**
```csharp
using WinAppDtudo.Services;

public partial class Frm_Existente : Form
{
    public Frm_Existente()
    {
        InitializeComponent();
        ThemeManager.ApplyDarkModeToForm(this);  // ← Adicione esta linha
    }
}
```

---

## 📌 Exemplo 3: Usar Cores do Tema em Customizações

```csharp
using WinAppDtudo.Services;

public partial class Frm_ComCustomizacoes : Form
{
    public Frm_ComCustomizacoes()
    {
        InitializeComponent();
        ThemeManager.ApplyDarkModeToForm(this);

        // Se precisar customizar um controle específico:
        myButton.BackColor = ThemeManager.GetThemeColor(ThemeColorType.Accent);
        myButton.ForeColor = Color.White;

        // Ou acessar diretamente as cores:
        myLabel.ForeColor = DarkModeColors.TextColor;
        myPanel.BackColor = DarkModeColors.BackgroundSecondaryColor;
    }
}
```

---

## 📌 Exemplo 4: Verificar o Status do Dark Mode

```csharp
using WinAppDtudo.Services;

public void ConfigurarInterface()
{
    if (ThemeManager.IsDarkModeEnabled)
    {
        // Você está no modo escuro

        // Exemplo: Ajustar ícones ou imagens para escuro
        myPictureBox.BackColor = DarkModeColors.BackgroundColor;
    }
    else
    {
        // Você está no modo claro (Light Mode)
        myPictureBox.BackColor = Color.White;
    }
}
```

---

## 📌 Exemplo 5: UserControl com Dark Mode

**Versão 1: Com Herança (Recomendado)**
```csharp
using WinAppDtudo.Helpers;

public partial class UC_MeuUserControl : BaseUserControlDarkMode
{
    public UC_MeuUserControl()
    {
        InitializeComponent();
        // Dark Mode aplicado automaticamente!
    }
}
```

**Versão 2: Com Função**
```csharp
using WinAppDtudo.Services;

public partial class UC_OutroUserControl : UserControl
{
    public UC_OutroUserControl()
    {
        InitializeComponent();
        ThemeManager.ApplyDarkModeToUserControl(this);
    }
}
```

---

## 📌 Exemplo 6: Dialog Form com Dark Mode

```csharp
using WinAppDtudo.Helpers;

public partial class Frm_MinhaDialog : BaseFormDarkMode
{
    public Frm_MinhaDialog()
    {
        InitializeComponent();
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
    }

    private void BtnOK_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void BtnCancel_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }
}
```

---

## 📌 Exemplo 7: Paleta de Cores Disponíveis

```csharp
using WinAppDtudo.Services;

// Acessar cores da paleta:
Color corFundo = DarkModeColors.BackgroundColor;              // #202020
Color corFundoSecundaria = DarkModeColors.BackgroundSecondaryColor;  // #2D2D2D
Color corTexto = DarkModeColors.TextColor;                    // #E5E5E5
Color corTextoSecundaria = DarkModeColors.TextSecondaryColor; // #9B9B9B
Color corBorda = DarkModeColors.BorderColor;                  // #464646
Color corDestaque = DarkModeColors.AccentColor;               // #0078D7 (Azure)
Color corDesabilitado = DarkModeColors.DisabledColor;         // #505050
Color corSucesso = DarkModeColors.SuccessColor;               // #10B070 (Verde)
Color corErro = DarkModeColors.ErrorColor;                    // #F04C4C (Vermelho)
Color corAviso = DarkModeColors.WarningColor;                 // #FF9F40 (Laranja)
Color corInfo = DarkModeColors.InfoColor;                     // #0096C8 (Azul)

// Ou usar a função helper:
Color cor = ThemeManager.GetThemeColor(ThemeColorType.Accent);
```

---

## 📌 Exemplo 8: Customizar MessageBox

Como os MessageBox padrão do .NET não têm dark mode, você pode criar um customizado:

```csharp
using WinAppDtudo.Services;

public static void MostrarMensagemCustomizada(string titulo, string mensagem)
{
    using (Form msgForm = new Form())
    {
        msgForm.Text = titulo;
        msgForm.Size = new Size(400, 150);
        msgForm.StartPosition = FormStartPosition.CenterScreen;
        msgForm.ShowInTaskbar = false;

        // Aplicar Dark Mode
        ThemeManager.ApplyDarkModeToForm(msgForm);

        Label lbl = new Label();
        lbl.Text = mensagem;
        lbl.Dock = DockStyle.Fill;
        msgForm.Controls.Add(lbl);

        msgForm.ShowDialog();
    }
}
```

---

## 📌 Exemplo 9: Panel com Dark Mode

```csharp
using WinAppDtudo.Services;

public partial class Frm_ComPaineis : Form
{
    public Frm_ComPaineis()
    {
        InitializeComponent();
        ThemeManager.ApplyDarkModeToForm(this);

        // Criar um painel dinamicamente
        Panel pnl = new Panel();
        pnl.BackColor = DarkModeColors.BackgroundSecondaryColor;
        pnl.ForeColor = DarkModeColors.TextColor;
        pnl.Dock = DockStyle.Top;
        pnl.Height = 50;

        Label lbl = new Label();
        lbl.Text = "Painel com Dark Mode";
        lbl.ForeColor = DarkModeColors.TextColor;
        pnl.Controls.Add(lbl);

        this.Controls.Add(pnl);
    }
}
```

---

## 📌 Exemplo 10: DataGridView com Dark Mode

```csharp
using WinAppDtudo.Helpers;

public partial class Frm_ComDataGrid : BaseFormDarkMode
{
    public Frm_ComDataGrid()
    {
        InitializeComponent();

        // Criar DataGridView dinamicamente
        DataGridView dgv = new DataGridView();
        dgv.Dock = DockStyle.Fill;

        // Adicionar colunas
        dgv.Columns.Add("Nome", "Nome");
        dgv.Columns.Add("Valor", "Valor");

        // Adicionar linhas
        dgv.Rows.Add("Item 1", "R$ 100,00");
        dgv.Rows.Add("Item 2", "R$ 200,00");

        // Aplicar tema via ThemeManager
        ThemeManager.ApplyDarkModeToControl(dgv);

        this.Controls.Add(dgv);
    }
}
```

---

## ✅ Resumo Rápido

| Cenário | Solução |
|---------|---------|
| Novo Form | `BaseFormDarkMode` |
| Form Existente | `ThemeManager.ApplyDarkModeToForm()` |
| Novo UserControl | `BaseUserControlDarkMode` |
| UC Existente | `ThemeManager.ApplyDarkModeToUserControl()` |
| Customizar Cor | `ThemeManager.GetThemeColor()` ou `DarkModeColors.*` |
| Verificar Dark Mode | `ThemeManager.IsDarkModeEnabled` |

---

## 🎓 Boas Práticas

✅ **SIM:**
- Sempre chame `ThemeManager.ApplyDarkMode...()` APÓS `InitializeComponent()`
- Use `BaseFormDarkMode` para novos formulários
- Acesse cores via `DarkModeColors.*` ou `ThemeManager.GetThemeColor()`

❌ **NÃO:**
- Não chame antes de `InitializeComponent()` - os componentes ainda não existem!
- Não crie múltiplas instâncias de gerenciadores
- Não modifique as cores em runtime a menos que seja necessário

---

**Última atualização**: 2025


