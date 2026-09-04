namespace CMP.Scripts
{
    /// <summary>
    /// Hayalet kişilikleri. Orijinal PAC-MAN'de dört hayaletin kodu aynıdır;
    /// aralarındaki tek fark takip sırasında hedefledikleri hücredir.
    /// </summary>
    public enum GhostPersonality
    {
        /// <summary>Doğrudan Pacman'in üstüne gider.</summary>
        Blinky,

        /// <summary>Pacman'in önünü keser.</summary>
        Pinky,

        /// <summary>Blinky'nin konumuna göre hesap yapar, öngörülemez.</summary>
        Inky,

        /// <summary>Uzaktayken takip eder, yaklaşınca köşesine kaçar.</summary>
        Clyde,
    }
}
