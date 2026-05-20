namespace RoclandGuardiaRelevo.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("RondinPage", typeof(Views.RondinPage));
            Routing.RegisterRoute("FirmaPage", typeof(Views.FirmaPage));
        }
    }
}
