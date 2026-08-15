using Scada_SortConveyor.Data;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Input;
namespace Scada_SortConveyor
{
    public partial class MainWindow : Window
    {
        private readonly PlcManager plc = new PlcManager();
        private readonly DispatcherTimer scadaTimer = new DispatcherTimer();
        private readonly DashboardModel dashboard = new DashboardModel();
        private readonly System.Diagnostics.Stopwatch chronoProduction = new System.Diagnostics.Stopwatch();

        private bool isBusy = false;
        private int piecesInitiales = 0;
        private bool _drawerIsOpen = false;
        private bool _isAnimating = false;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = dashboard;

            BtnConnecter.Click += BtnConnecter_Click;
            BtnMarche.Click += BtnMarche_Click;
            BtnArret.Click += BtnArret_Click;
            BtnAcquittement.Click += BtnAcquittement_Click;

            plc.ErreurSurvenue += Plc_ErreurSurvenue;

            scadaTimer.Interval = TimeSpan.FromMilliseconds(200); // 200ms = 5 Hz, suffisant
            scadaTimer.Tick += ScadaTimer_Tick;
        }

        private void AlarmHeaderBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_drawerIsOpen || _isAnimating) return;
            _isAnimating = true;

            SyncAlarmLists();
            AlarmOverlay.Visibility = Visibility.Visible;

            var sb = (Storyboard)FindResource("OpenAlarmDrawer");

            // Quand l'animation est terminée
            sb.Completed += (s, args) =>
            {
                _drawerIsOpen = true;
                _isAnimating = false;
            };

            sb.Begin();
        }

        private void CloseAlarmDrawer_Click(object sender, RoutedEventArgs e)
        {
            CloseAlarmDrawer();
        }

        private void AlarmOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseAlarmDrawer();
        }

        private void CloseAlarmDrawer()
        {
            if (!_drawerIsOpen || _isAnimating) return;
            _isAnimating = true;

            var sb = (Storyboard)FindResource("CloseAlarmDrawer");

            sb.Completed += (s, args) =>
            {
                _drawerIsOpen = false;
                _isAnimating = false;
                // Sécurité supplémentaire
                AlarmOverlay.Visibility = Visibility.Collapsed;
            };

            sb.Begin();
        }



        private void Plc_ErreurSurvenue(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LblStatus.Text = message;
                LblStatus.Foreground = new SolidColorBrush(Colors.Orange);
            });
        }
        

        // Fermer via le bouton ✕
        

        // Fermer en cliquant sur le fond assombri
        

        

        // Synchronise la ListBox compacte et la ListBox détaillée
        private void SyncAlarmLists()
        {
            LstAlarmesDetail.Items.Clear();
            foreach (var item in LstAlarmes.Items)
            {
                LstAlarmesDetail.Items.Add(item);
            }
            TxtAlarmCount.Text = $"{LstAlarmesDetail.Items.Count} alarme(s) active(s)";
        }
        private void BtnConnecter_Click(object sender, RoutedEventArgs e)
        {
            if (plc.Connecter(TxtIpAddress.Text))
            {
                LedConnexion.Fill = new SolidColorBrush(Colors.LimeGreen);
                LblStatus.Text = "Connecté";
                LblStatus.Foreground = new SolidColorBrush(Colors.LimeGreen);
                scadaTimer.Start();
            }
            else
            {
                LedConnexion.Fill = new SolidColorBrush(Colors.DarkRed);
                LblStatus.Text = "Hors ligne";
                LblStatus.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private async void BtnMarche_Click(object sender, RoutedEventArgs e)
        {
            if (plc.myPlc == null || !plc.myPlc.IsConnected) return;

            piecesInitiales = plc.LireVariableUShort("DB3.DBW0") +
                              plc.LireVariableUShort("DB3.DBW2") +
                              plc.LireVariableUShort("DB3.DBW4");

            plc.EcrireVariableBool("DB6.DBX0.0", true);
            await Task.Delay(300);
            plc.EcrireVariableBool("DB6.DBX0.0", false);

            chronoProduction.Start();
        }

        private async void BtnArret_Click(object sender, RoutedEventArgs e)
        {
            if (plc.myPlc == null || !plc.myPlc.IsConnected) return;

            plc.EcrireVariableBool("DB6.DBX0.1", true);
            await Task.Delay(300);
            plc.EcrireVariableBool("DB6.DBX0.1", false);

            chronoProduction.Stop();
        }

        private async void BtnAcquittement_Click(object sender, RoutedEventArgs e)
        {
            if (plc.myPlc == null || !plc.myPlc.IsConnected) return;

            plc.EcrireVariableBool("DB6.DBX0.2", true);
            await Task.Delay(300);
            plc.EcrireVariableBool("DB6.DBX0.2", false);
        }

        private void ScadaTimer_Tick(object? sender, EventArgs e)
        {
            if (isBusy) return; // Évite l'empilement des ticks
            isBusy = true;

            try
            {
                if (!plc.MaintenirConnexion()) return;

                // --- LECTURE PLC ---
                bool presenceEntree = plc.LireVariableBool("DB6.DBX0.3");
                bool moteurEnMarche = plc.LireVariableBool("DB6.DBX0.5");
                bool cmdEjectionCourte = plc.LireVariableBool("DB6.DBX0.7");
                bool cmdEjectionLongue = plc.LireVariableBool("DB6.DBX1.0");

                ushort typeDetect = plc.LireVariableUShort("DB6.DBW2");
                ushort registreAlarmes = plc.LireVariableUShort("DB8.DBW0");

                ushort nbCourtes = plc.LireVariableUShort("DB3.DBW0");
                ushort nbLongues = plc.LireVariableUShort("DB3.DBW2");
                ushort nbRejets = plc.LireVariableUShort("DB3.DBW4");

                // --- DÉCODAGE ALARMES ---
                bool defautCapteurEntree = (registreAlarmes & (1 << 0)) != 0;
                bool defautCourroie = (registreAlarmes & (1 << 1)) != 0;
                bool defautCapteurPoids = (registreAlarmes & (1 << 2)) != 0;
                bool alarmeBourrage = (registreAlarmes & (1 << 3)) != 0;
                bool defautVerinCourt = (registreAlarmes & (1 << 4)) != 0;
                bool defautVerinLong = (registreAlarmes & (1 << 5)) != 0;

                // --- MISE À JOUR UI (Dispatcher implicite car DispatcherTimer) ---
                MettreAJourAffichageAlarmes(defautCapteurEntree, defautCourroie, defautCapteurPoids,
                                            alarmeBourrage, defautVerinCourt, defautVerinLong);

                LedMoteur.Fill = moteurEnMarche
                    ? new SolidColorBrush(Colors.LimeGreen)
                    : new SolidColorBrush(Color.FromRgb(85, 85, 85));

                LedPresence.Fill = presenceEntree
                    ? new SolidColorBrush(Colors.Yellow)
                    : new SolidColorBrush(Color.FromRgb(51, 51, 51));

                VoyantVerinCourt.Fill = cmdEjectionCourte
                    ? new SolidColorBrush(Colors.SteelBlue)
                    : new SolidColorBrush(Color.FromRgb(51, 51, 51));

                VoyantVerinLong.Fill = cmdEjectionLongue
                    ? new SolidColorBrush(Colors.Red)
                    : new SolidColorBrush(Color.FromRgb(51, 51, 51));

                switch (typeDetect)
                {
                    case 1:
                        TxtTypeDetecte.Text = "PIÈCE COURTE";
                        TxtTypeDetecte.Foreground = new SolidColorBrush(Color.FromRgb(3, 169, 244));
                        break;
                    case 2:
                        TxtTypeDetecte.Text = "PIÈCE LONGUE";
                        TxtTypeDetecte.Foreground = new SolidColorBrush(Color.FromRgb(229, 57, 53));
                        break;
                    case 3:
                        TxtTypeDetecte.Text = "REJET / PARASITE";
                        TxtTypeDetecte.Foreground = new SolidColorBrush(Colors.Gray);
                        break;
                    default:
                        TxtTypeDetecte.Text = "EN ATTENTE...";
                        TxtTypeDetecte.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                        break;
                }

                // --- GRAPHIQUES (ObservableValue se met à jour tout seul) ---
                int piecesAcceptees = nbCourtes + nbLongues;
                int piecesRejetees = nbRejets;
                int totalPieces = piecesAcceptees + piecesRejetees;

                dashboard.ValCourtes.Value = nbCourtes;
                dashboard.ValLongues.Value = nbLongues;
                dashboard.ValRejets.Value = nbRejets;
                dashboard.ValAcceptees.Value = piecesAcceptees;
                dashboard.ValRejetees.Value = piecesRejetees;

                TxtTotalPieces.Text = totalPieces.ToString();

                if (totalPieces == 0)
                    TxtTauxQualite.Text = "0.0 %";
                else
                    TxtTauxQualite.Text = $"{((float)piecesAcceptees / totalPieces * 100):F1} %";

                // Debug visuel dans la sortie Visual Studio (Ctrl+Alt+O)
                System.Diagnostics.Debug.WriteLine(
                    $"[SCADA] Moteur={moteurEnMarche}, Presence={presenceEntree}, " +
                    $"EjectionC={cmdEjectionCourte}, EjectionL={cmdEjectionLongue}, " +
                    $"Courtes={nbCourtes}, Longues={nbLongues}, Rejets={nbRejets}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERREUR SCADA] {ex.Message}");
            }
            finally
            {
                isBusy = false;
            }
        }

        private void MettreAJourAffichageAlarmes(bool capteurEntree, bool courroie, bool capteurPoids,
                                                 bool bourrage, bool verinCourt, bool verinLong)
        {
            string horodatage = DateTime.Now.ToString("HH:mm:ss");
            LstAlarmes.Items.Clear();

            if (capteurEntree) LstAlarmes.Items.Add($"[{horodatage}] ▶ DÉFAUT : Capteur Entrée aveuglé");
            if (courroie) LstAlarmes.Items.Add($"[{horodatage}] ▶ DÉFAUT : Pièce perdue (Courroie)");
            if (capteurPoids) LstAlarmes.Items.Add($"[{horodatage}] ▶ DÉFAUT : Capteur Pesée HS");
            if (bourrage) LstAlarmes.Items.Add($"[{horodatage}] ▶ ALARME : Bourrage en fin de ligne");
            if (verinCourt) LstAlarmes.Items.Add($"[{horodatage}] ▶ DÉFAUT : Vérin Court bloqué");
            if (verinLong) LstAlarmes.Items.Add($"[{horodatage}] ▶ DÉFAUT : Vérin Long bloqué");

            if (LstAlarmes.Items.Count == 0)
            {
                LstAlarmes.Items.Add("✓ SYSTÈME OK - Aucun défaut");
                LstAlarmes.Foreground = new SolidColorBrush(Colors.LimeGreen);
                if (LstAlarmes.Parent is Border b) b.Background = new SolidColorBrush(Color.FromRgb(0, 30, 0));
            }
            else
            {
                LstAlarmes.Foreground = new SolidColorBrush(Colors.Red);
                if (LstAlarmes.Parent is Border b) b.Background = new SolidColorBrush(Color.FromRgb(43, 0, 0));
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            scadaTimer.Stop();

            if (plc.myPlc != null && plc.myPlc.IsConnected)
                plc.myPlc.Close();

            Environment.Exit(0);
        }
    }
}