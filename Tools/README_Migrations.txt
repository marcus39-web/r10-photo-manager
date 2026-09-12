Um Entity Framework Core Migrationen lokal zu verwenden (Development):

1) Stellen Sie sicher, dass die NuGet-Pakete wiederhergestellt sind:
   dotnet restore

2) Installieren Sie dotnet-ef global (optional, wenn nicht bereits installiert):
   dotnet tool install --global dotnet-ef

3) Erstellen Sie eine Initial-Migration:
   dotnet ef migrations add InitialCreate --project D:\\11_Foto_App\\R10CSharp\\R10CSharp.csproj

4) Wenden Sie die Migration an (erzeugt Tabellen in der konfigurierten LocalDB):
   dotnet ef database update --project D:\\11_Foto_App\\R10CSharp\\R10CSharp.csproj

Hinweis: In der App habe ich Database.EnsureCreated() gesetzt. Für Migrationen ändern Sie bitte EnsureCreated() zu Database.Migrate() in App.xaml.cs, damit Migrationen beim Start angewendet werden.
