namespace AshClicker
{
    public class AshKeypress
    {
        public string Name { get; }
        public AshKey UserPress { get; }
        public AshKey WillPress { get; }
        public int Mode { get; }

        public AshKeypress(string name, AshKey userPress, AshKey willPress, int mode)
        {
            Name = name;
            UserPress = userPress;
            WillPress = willPress;
            Mode = mode;
        }

        /// <summary>紧凑的绑定关系，例如 1|Q|切换。</summary>
        public string BindingText
        {
            get
            {
                var modeText = Mode == 1 ? "切换" : "连发";
                return UserPress.ShortName + "|" + WillPress.ShortName + "|" + modeText;
            }
        }
    }
}
