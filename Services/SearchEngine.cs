using System;
using System.Collections.Generic;
using System.Linq;
using R10CSharp.Models;

namespace R10CSharp.Services
{
    /// <summary>
    /// Einfache In-Memory-Suche über den geladenen Fotoindex.
    /// Die SearchPage ergänzt darauf aufbauend Datums-, Ordner- und UI-Filter.
    /// </summary>
    public class SearchEngine(List<PhotoIndexEntry> index)
    {
        private readonly List<PhotoIndexEntry> _index = index ?? [];

        // Akzeptiert nullable Filter-Parameter, damit UI-Aufrufer ohne Zusatzkonvertierung suchen können.
        public List<PhotoIndexEntry> Search(string? query, string? category, string? rawOrJpg, string? series, string? favorites)
        {
            IEnumerable<PhotoIndexEntry> q = _index;

            if (!string.IsNullOrWhiteSpace(query))
            {
                // Freitextsuche aktuell über Dateiname und Tags.
                q = q.Where(i => i.FileName.Contains(query, StringComparison.OrdinalIgnoreCase)
                                 || (i.Tags ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            // Exakte Zusatzfilter aus der UI.
            if (!string.IsNullOrWhiteSpace(category)) q = q.Where(i => string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(rawOrJpg)) q = q.Where(i => string.Equals(i.RawOrJpg, rawOrJpg, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(series)) q = q.Where(i => string.Equals(i.Series, series, StringComparison.OrdinalIgnoreCase));

            return [.. q];
        }
    }
}
