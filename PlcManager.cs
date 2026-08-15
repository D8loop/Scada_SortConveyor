using S7.Net;
using System;
using System.Text.RegularExpressions;

namespace Scada_SortConveyor
{
    public class PlcManager
    {
        // ✅ Nullable : le PLC n'est pas initialisé dans le constructeur
        public Plc? myPlc;

        // ✅ Nullable : l'événement peut ne pas avoir d'abonné au départ
        public event Action<string>? ErreurSurvenue;

        private void SignalerErreur(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[PLC] {message}");
            ErreurSurvenue?.Invoke(message);
        }

        public bool Connecter(string adresseIp)
        {
            myPlc = new Plc(CpuType.S71500, adresseIp, 0, 1);
            try
            {
                myPlc.Open();
                if (myPlc.IsConnected) return true;

                SignalerErreur("PLC non connecté (Open() a réussi mais IsConnected = false).");
                return false;
            }
            catch (Exception ex)
            {
                SignalerErreur($"Erreur connexion : {ex.Message}");
                return false;
            }
        }

        // ==========================================
        // LECTURE BOOLÉENNE FIABLE (S7-1500)
        // ==========================================
        public bool LireVariableBool(string adresse)
        {
            if (myPlc == null || !myPlc.IsConnected) return false;

            try
            {
                return LireBoolViaBytes(adresse);
            }
            catch (Exception ex)
            {
                SignalerErreur($"Erreur lecture bool {adresse} : {ex.Message}");
                return false;
            }
        }

        private bool LireBoolViaBytes(string adresse)
        {
            var match = Regex.Match(adresse, @"DB(\d+)\.DBX(\d+)\.(\d+)");
            if (!match.Success)
                throw new FormatException($"Format attendu: DB<num>.DBX<byte>.<bit> (reçu: {adresse})");

            int db = int.Parse(match.Groups[1].Value);
            int byteAddr = int.Parse(match.Groups[2].Value);
            int bitAddr = int.Parse(match.Groups[3].Value);

            // ✅ ReadBytes peut retourner null
            byte[]? bytes = myPlc!.ReadBytes(DataType.DataBlock, db, byteAddr, 1);
            if (bytes == null || bytes.Length == 0) return false;

            return (bytes[0] & (1 << bitAddr)) != 0;
        }

        public ushort LireVariableUShort(string adresse)
        {
            if (myPlc == null || !myPlc.IsConnected) return 0;
            try
            {
                // ✅ Read retourne object?
                object? result = myPlc.Read(adresse);
                if (result == null) return 0;

                if (result is ushort u) return u;
                if (result is short s) return (ushort)s;
                if (result is int i) return (ushort)i;
                if (result is uint ui) return (ushort)ui;
                return Convert.ToUInt16(result);
            }
            catch (Exception ex)
            {
                SignalerErreur($"Erreur lecture ushort {adresse} : {ex.Message}");
                return 0;
            }
        }

        public float LireVariableReel(string adresse)
        {
            if (myPlc == null || !myPlc.IsConnected) return 0.0f;
            try
            {
                object? result = myPlc.Read(adresse);
                if (result == null) return 0.0f;
                return (float)result;
            }
            catch (Exception ex)
            {
                SignalerErreur($"Erreur lecture float {adresse} : {ex.Message}");
                return 0.0f;
            }
        }

        public bool EcrireVariableBool(string adresse, bool valeur)
        {
            if (myPlc == null || !myPlc.IsConnected)
            {
                SignalerErreur($"Écriture refusée sur {adresse} : PLC non connecté.");
                return false;
            }
            try
            {
                myPlc.Write(adresse, valeur);
                return true;
            }
            catch (Exception ex)
            {
                SignalerErreur($"Erreur écriture bool {adresse} : {ex.Message}");
                return false;
            }
        }

        public bool EcrireVariableReel(string adresse, float valeur)
        {
            if (myPlc == null || !myPlc.IsConnected) return false;
            try { myPlc.Write(adresse, valeur); return true; }
            catch (Exception ex)
            {
                SignalerErreur($"Erreur écriture float {adresse} : {ex.Message}");
                return false;
            }
        }

        public bool MaintenirConnexion()
        {
            if (myPlc == null) return false;
            if (myPlc.IsConnected) return true;

            SignalerErreur("Connexion perdue ! Tentative de reconnexion...");

            try
            {
                myPlc.Close();
                System.Threading.Thread.Sleep(500);
                myPlc.Open();

                if (myPlc.IsConnected)
                {
                    SignalerErreur("Reconnexion réussie !");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                SignalerErreur($"Échec reconnexion : {ex.Message}");
                return false;
            }
        }
    }
}