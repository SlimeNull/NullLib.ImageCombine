using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NullLib.CommandLine;
using NullLib.Drawing;

namespace Null.ImageCombine.Cmd
{
    class Program
    {
        class MyCommands : CommandHome
        {
            string output;
            int width, height, column;
            bool horizontalFirst, mainReverse, crossReverse;
            List<string> imgs = new List<string>();
            ImageSizingMode sizingMode;
            private int row;
            Color backColor;
            RotateFlipType rotate;

            Lazy<ColorConverter> ColorConverter = new Lazy<ColorConverter>();

            private void ProgressReporter(DealState state, int now, int total)
            {
                Console.WriteLine($"[.] {state} - {now}/{total}");
            }

            [Command(typeof(IntArguConverter))]
            public void SetWidth(int width) => this.width = width;
            [Command(typeof(IntArguConverter))]
            public void SetHeight(int height) => this.height = height;
            [Command(typeof(IntArguConverter))]
            public void SetColumn(int column) => this.column = column;
            [Command(typeof(IntArguConverter))]
            public void SetRow(int row) => this.row = row;
            [Command(typeof(BoolArguConverter))]
            public void SetHorizontalFirst(bool value) => horizontalFirst = value;
            [Command(typeof(BoolArguConverter))]
            public void SetMainReverse(bool value) => mainReverse = value;
            [Command(typeof(BoolArguConverter))]
            public void SetCrossReverse(bool value) => crossReverse = value;
            [Command(typeof(EnumArguConverter<ImageSizingMode>))]
            public void SetSizingMode(ImageSizingMode sizingMode) => this.sizingMode = sizingMode;
            [Command(typeof(ArguConverter))]
            public void AppendInput(string path) => imgs.Add(path);
            [Command(CommandAlias = "ClearInput")]
            public void ClearInputs() => imgs.Clear();
            [Command(typeof(ArguConverter))]
            public void SetOutput(string path) => output = path;
            [Command(typeof(ArguConverter))]
            public void SetBackColor(string backColor) => this.backColor = (Color)ColorConverter.Value.ConvertFromString(backColor);
            [Command(typeof(IntArguConverter))]
            public void SetRotate(int deg)
            {
                rotate = deg switch
                {
                    90 => RotateFlipType.Rotate90FlipNone,
                    180 => RotateFlipType.Rotate180FlipNone,
                    270 => RotateFlipType.Rotate270FlipNone,
                    -90 => RotateFlipType.Rotate270FlipNone,
                    -180 => RotateFlipType.Rotate180FlipNone,
                    -270 => RotateFlipType.Rotate90FlipNone,
                    
                    _ => throw new Exception("[!] Rotation degree must be one of (90, 180, 270, -90, -180, -270)")
                };
            }
            [Command]
            public void StartCombine()
            {
                try
                {
                    ImageCombineLib.Combine(imgs.ToArray(), ProgressReporter, out var outputBmp, sizingMode, rotate, PixelFormat.Format32bppArgb, backColor, width, height, column, row, horizontalFirst, mainReverse, crossReverse);
                    if (outputBmp is null)
                    {
                        Console.WriteLine("[!] Combination result is null");
                        return;
                    }
                    outputBmp.Save(output);
                    Environment.ExitCode = 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Combine failed. {ex.GetType()}: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    Environment.ExitCode = -1;
                }
            }

            [Command]
            public void Exit() => Environment.Exit(0);
            [Command]
            public void Help()
            {
                Console.WriteLine(CommandObject.GenCommandOverviewText());
            }
        }
        static CommandObject<MyCommands> CommandObject { get; } = new CommandObject<MyCommands>();
        static void Main(string[] args)
        {
            bool useOpen = !Console.IsInputRedirected;
            if (useOpen)
                Console.WriteLine("Null.ImageCombine v1.0.1 by SlimeNull\n");
            while (true)
            {
                if (useOpen)
                    Console.Write(">>> ");
                string todo = Console.ReadLine();
                if (todo is null)
                    return;
#if DEBUG
                    CommandObject.ExecuteCommand(todo, true);
#else
                try
                {
                    CommandObject.ExecuteCommand(todo, true);
                }
                catch (TargetInvocationException tiex)
                {
                    Console.WriteLine($"[!] Command execute failed: {tiex.InnerException.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Command execute failed: {ex.Message}");
                }
#endif
            }
        }
    }
}
