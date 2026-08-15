using System;
using System.Data.SQLite;
using System.IO;

namespace Scada_SortConveyor
{
    public class DatabaseManager
    {
        // Le chemin vers le fichier de la base de données
        private readonly string dbPath = "HistoriqueScada.db";
        // La chaîne de connexion que SQLite utilise pour trouver le fichier
        private readonly string connectionString;

        public DatabaseManager()
        {
            // On construit la chaîne de connexion
            connectionString = $"Data Source={dbPath};Version=3;";
            InitialiserBaseDeDonnees();
        }

        private void InitialiserBaseDeDonnees()
        {
            // Si le fichier de la base n'existe pas, on le crée
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
                System.Diagnostics.Debug.WriteLine("[DB] Fichier de base de données créé.");
            }

            // On crée la table 'Alarmes' si elle n'existe pas déjà
            using (var connexion = new SQLiteConnection(connectionString))
            {
                connexion.Open();

                string creationTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Alarmes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DateHeure DATETIME NOT NULL,
                        Message TEXT NOT NULL,
                        Etat TEXT NOT NULL
                    )";

                using (var commande = new SQLiteCommand(creationTableQuery, connexion))
                {
                    commande.ExecuteNonQuery();
                }
            }
        }

        // Méthode pour ajouter une alarme dans la base
        public void EnregistrerAlarme(string message, string etat)
        {
            try
            {
                using (var connexion = new SQLiteConnection(connectionString))
                {
                    connexion.Open();

                    // Requête SQL paramétrée (très important pour la sécurité !)
                    string insertQuery = "INSERT INTO Alarmes (DateHeure, Message, Etat) VALUES (@date, @msg, @etat)";

                    using (var commande = new SQLiteCommand(insertQuery, connexion))
                    {
                        // On remplit les paramètres
                        commande.Parameters.AddWithValue("@date", DateTime.Now);
                        commande.Parameters.AddWithValue("@msg", message);
                        commande.Parameters.AddWithValue("@etat", etat); // Par exemple "Apparue" ou "Disparue"

                        commande.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERREUR DB] Impossible d'enregistrer l'alarme : {ex.Message}");
            }
        }
    }
}