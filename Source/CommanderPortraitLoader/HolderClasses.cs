namespace CommanderPortraitLoader {
    public class CustomPreset {
        public CustomDescription Description = new CustomDescription();
        public bool isCommander;

        // BEN: This determines gender!
        // PortraitSettings: Gender: get: return (this.headMesh > 0.4f) ? ((this.headMesh <= 0.6f) ? Gender.NonBinary : Gender.Female) : Gender.Male;
        public float headMesh = 0.5f;
    }

    public class CustomDescription {
        public string Id;
        public string Name;
        public string Details;
        public string Icon;
    }
}