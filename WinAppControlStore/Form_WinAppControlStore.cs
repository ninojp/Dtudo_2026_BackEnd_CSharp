namespace WinAppControlStore
{
    public partial class Form_WinAppControlStore : Form
    {
        public Form_WinAppControlStore()
        {
            InitializeComponent();
        }

        private void Btn_Site_Dtudo_Click(object sender, EventArgs e)
        {
            //Futuramente vai carregar todo FrontEnd.
        }

        private void Btn_Sair_App_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Btn_Modifica_Label_Click(object sender, EventArgs e)
        {
            Lbl_Titulo.Text = Txb_Texto_Temp.Text;
        }
    }
}
