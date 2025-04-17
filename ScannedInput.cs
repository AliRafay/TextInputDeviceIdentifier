namespace TextInputDeviceIdentifier
{
    public class ScannedInput
    {
        public string DeviceId { get; set; }
        public string Input { get; set; }

        public ScannedInput(string deviceId)
        {
            DeviceId = deviceId;
            Input = string.Empty;
        }
    }
}