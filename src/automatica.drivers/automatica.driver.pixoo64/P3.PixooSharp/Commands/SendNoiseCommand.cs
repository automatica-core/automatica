namespace P3.PixooSharp.Commands
{
    internal class SendNoiseCommand
    {
        public string Command { get; } = "Tools/SetNoiseStatus";
        public int NoiseStatus { get; set; }

        public SendNoiseCommand(bool status)
        {
            if (status)
            {
                NoiseStatus = 1;
            }
            else
            {
                NoiseStatus = 0;
            }
            
        }
    }
}
