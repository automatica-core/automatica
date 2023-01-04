using System.Text;
using System.Text.Json;
using P3.PixooSharp.Assets;
using P3.PixooSharp.Commands;

namespace P3.PixooSharp
{
    public class Pixoo64
    {
        private readonly string _url;
        private readonly int _screenSize;
        private readonly bool _debug;
        private readonly int _pixelCount;
        private readonly Font _pico8;
        private int _pushCount = 1;
        private int _textId = 0;

        private Rgb[] _buffer;

        public Pixoo64(string address, int size, bool debug = false, int pushCount = 50)
        {
            _url = string.Format("http://{0}/post", address);
            _debug = debug;
            _screenSize = size;
            _pushCount = pushCount;
            switch (size)
            {
                case 16:
                case 32:
                case 64:
                    _pixelCount = size * size;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("size needs to be 16, 32 or 64", size.ToString());
            }
            _buffer = new Rgb[_screenSize * _screenSize];
            _pico8 = new Font();
            // Set default boot colour to black
            Fill(Palette.Black);
        }

        public void Clear()
        {
            Fill(Palette.Black);
        }

        public void Fill(Rgb fillColour)
        {
            for (int buffPos = 0; buffPos < _pixelCount; buffPos += 1)
            {
                _buffer[buffPos] = fillColour;
            }
        }

        public void DrawFilledRectangle(int topX, int topY, int bottomX, int bottomY, Rgb colour)
        {
            for (var y = topY; y <= bottomY; y++)
            {
                for (var x = topX; x <= bottomX; x++)
                {
                    DrawPixel(x, y, colour);
                }
            }
        }

        public void DrawLine(int startX, int startY, int endX, int endY, Rgb colour)
        {
            // Calculate the amount of steps needed between the points to draw a nice line
            int amountOfSteps = MinimumAmountOfSteps(startX, startY, endX, endY);

            // Iterate over them and create a nice set of pixels
            for (int step = 0; step < amountOfSteps; step++)
            {
                decimal interpolant = 0;
                if (amountOfSteps != 0)
                {
                    interpolant = ((decimal)step / (decimal)amountOfSteps);
                }
                // Add a pixel as a rounded location
                Coord lerpResult = LerpLocation(startX, startY, endX, endY, interpolant);
                Coord roundResult = RoundLocation(lerpResult.X, lerpResult.Y);
                DrawPixel((int)roundResult.X, (int)roundResult.Y, colour);
            }
        }

        public decimal Lerp(decimal start, decimal end, decimal interpolant)
        {
            return (start + interpolant * (end - start));
        }

        public Coord LerpLocation(decimal startX, decimal startY, decimal endX, decimal endY, decimal interpolant)
        {
            Coord coord = new Coord()
            {
                X = Lerp(startX, endX, interpolant),
                Y = Lerp(startY, endY, interpolant)
            };
            return coord;
        }

        public Coord RoundLocation(decimal x, decimal y)
        {
            Coord result = new Coord()
            {
                X = (int)Math.Round(x),
                Y = (int)Math.Round(y)
            };
            return result;
        }

        public int MinimumAmountOfSteps(int startX, int startY, int endX, int endY)
        {
            return Math.Max(Math.Abs(startX - endX), Math.Abs(startY - endY));
        }

        public void DrawText(int x, int y, Rgb colour, string text)
        {
            var characters = text.ToCharArray();
            int index = 0;
            foreach (char c in characters)
            {
                if (c != ' ')
                {
                    DrawCharacter((index * 4) + x, y, colour, c);
                }
                index++;
            }
        }
        public void DrawCharacter(int x,int y, Rgb colour, char character)
        {
            var matrix = _pico8.GetCharacterMatrix(character);
            if (matrix != null)
            {
                for (int index = 0; index < matrix.Length; index++)
                {
                    var bit = matrix[index];
                    if (bit == 1)
                    {
                        int local_x = index % 3;
                        int local_y = (index / 3);
                        DrawPixel(x + local_x, y + local_y, colour);
                    }
                }
            }
        }

        public void DrawPixel(int x, int y, Rgb colour)
        {
            if (x >= 0 && x < _screenSize && y >= 0 && y < _screenSize)
            {
                var index = x + (y * _screenSize);
                DrawPixelAtIndex(index, colour);
            }
            else
            {
                if (_debug)
                {
                    int limit = _screenSize - 1;
                    Console.WriteLine("Error: Invalid coordinates given: ({0}, {1}) (maximum coordinates are ({3}, {3})", x, y, limit);
                }
            }
        }

        public void DrawPixelAtIndex(int index, Rgb colour)
        {
            if (index < 0 || index >= _pixelCount)
            {
                if (_debug)
                {
                    var limit = _pixelCount - 1;
                    Console.WriteLine("Error: Invalid index given: ({0}) (maximum coordinates are ({1})", index, limit);
                }
            } 
            else
            {
                _buffer[index] = colour;
            }
        }

        public async Task SendBufferAsync(int frameId)
        {
            // generate pixoo format image
            Byte[] sendBuffer = new byte[_pixelCount*3];
            int ySkip = _screenSize * 3;
            for (var y=0; y < _screenSize; y++)
            {
                if(_debug)
                    Console.Write(y.ToString());
                for (var x=0; x < _screenSize; x++)
                {
                    if (_debug)
                        Console.Write(".");
                    int sendIndex = (y * ySkip) + (x * 3);
                    int bufferIndex = (y * _screenSize) + x;
                    sendBuffer[sendIndex] = _buffer[bufferIndex].Red;
                    sendBuffer[sendIndex + 1] = _buffer[bufferIndex].Green;
                    sendBuffer[sendIndex + 2] = _buffer[bufferIndex].Blue;
                }
                if (_debug)
                    Console.WriteLine();
            }
            string base64String = Convert.ToBase64String(sendBuffer, 0, sendBuffer.Length);
            var command = new SendBufferCommand(_screenSize, frameId, base64String);
            var jsonString = JsonSerializer.Serialize(command);
            await SendCommandAsync(jsonString);
        }

        public async Task SendTextAsync(int x, int y, Direction direction, string text, Rgb colour, int font = 2, int textWidth = 64, int textSpeed = 0)
        {
            var command = new SendTextCommand(_textId, x, y, direction, text, colour, font, textWidth, textSpeed);
            var jsonCommand = JsonSerializer.Serialize(command);
            await SendCommandAsync(jsonCommand);
        }

        public async Task SendClearTextAsync()
        {
            var command = new SendClearTextCommand();
            var jsonCommand = JsonSerializer.Serialize(command);
            await SendCommandAsync(jsonCommand);
        }

        public async Task SendNoise(bool sounding)
        {
            var command = new SendNoiseCommand(sounding);
            var jsonCommand = JsonSerializer.Serialize(command);
            await SendCommandAsync(jsonCommand);
        }

        public async Task SendResetGif()
        {
            var command = new SendResetGifCommand();
            var jsonCommand = JsonSerializer.Serialize(command);
            await SendCommandAsync(jsonCommand);
        }

        public async Task SendCommandAsync(string jsonPayload)
        {
            if (_debug)
            {
                Console.WriteLine(jsonPayload);
            }
            using (var httpClient = new HttpClient())
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(_url, content);
                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync();
                    _pushCount++;
                }
                else
                {
                    // Something went wrong
                    Console.WriteLine("error!");
                }
            }
        }
    }
}
