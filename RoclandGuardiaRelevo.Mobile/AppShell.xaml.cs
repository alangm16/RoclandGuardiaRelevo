using RoclandGuardiaRelevo.Mobile.Views;

namespace RoclandGuardiaRelevo.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("RondinPage", typeof(Views.RondinPage));
            Routing.RegisterRoute("DetalleRondinPage", typeof(Views.DetalleRondinPage));
            // Routing.RegisterRoute("IncidenciasPage", typeof(Views.IncidenciasPage));
            Routing.RegisterRoute("FirmaPage", typeof(Views.FirmaPage));
        }
    }
}