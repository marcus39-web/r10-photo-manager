Packaging-Hinweise
==================

1. Dieses Projekt ergänzt eine Windows-App-Paketierung für R10CSharp.
2. Vor dem Signieren muss der Publisher im Package.appxmanifest exakt zum Zertifikat passen.
   Aktuell steht dort: CN=Marcus39Web
3. Die Paket-Assets verlinken vorläufig auf Assets\welcome.JPEG.
   Für eine saubere Store-/Startmenü-Darstellung sollten später echte quadratische PNG/JPG-Assets ersetzt werden.
4. Danach das Packaging-Projekt als Start-/Packaging-Projekt in Visual Studio verwenden und ein MSIX erzeugen/installieren.
