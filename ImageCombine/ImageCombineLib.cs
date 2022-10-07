using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NullLib.Drawing
{
    public enum DealState
    {
        ProcessParameters,
        ProcessLoadImages,
        ProcessImageSizes,
        ProcessDrawing,
        ProcessReturn,
        ProcessSave,
    }
    public enum ImageSizingMode
    {
        None,
        Center,
        Fill,
        Uniform,
        UniformToFill,
    }
    public static class ImageCombineLib
    {
        private static Rectangle CalcImageAbsBounds(ImageSizingMode sizingMode,int boxWidth, int boxHeight, int imgWidth, int imgHeight)
        {
            int
                left, width, top, height;

            (left, width, top, height) = sizingMode switch
            {
                ImageSizingMode.None => (0, imgWidth, 0, imgHeight),
                ImageSizingMode.Center => ((boxWidth - imgWidth) / 2, imgWidth, (boxHeight - imgHeight) / 2, imgHeight),
                ImageSizingMode.Fill => (0, boxWidth, 0, boxHeight),
                ImageSizingMode.Uniform => CalcUniformBounds(imgWidth, imgHeight, boxWidth, boxHeight),
                ImageSizingMode.UniformToFill => CalcUniformToFillBounds(imgWidth, imgHeight, boxWidth, boxHeight),
                _ => (0, imgWidth, 0, imgHeight)
            };

            return new Rectangle(left, top, width, height);

            (int, int, int, int) CalcUniformBounds(int imgWidth, int imgHeight, int parWidth, int parHeight)
            {
                int testHeight = parWidth * imgHeight / imgWidth;
                if (testHeight >= parHeight)
                    return (0, parWidth, (parHeight - testHeight) / 2, testHeight);
                int testWidth = parHeight * imgWidth / imgHeight;
                return ((parWidth - testWidth) / 2, testWidth, 0, parHeight);
            }
            (int, int, int, int) CalcUniformToFillBounds(int imgWidth, int imgHeight, int parWidth, int parHeight)
            {
                int testHeight = parWidth * imgHeight / imgWidth;
                if (testHeight <= parHeight)
                    return (0, parWidth, (parHeight - testHeight) / 2, testHeight);
                int testWidth = parHeight * imgWidth / imgHeight;
                return ((parWidth - testWidth) / 2, testWidth, 0, parHeight);
            }
        }
        public static int Combine(string[] images, Action<DealState, int, int> progressReporter, out Bitmap result, ImageSizingMode sizingMode = 0, RotateFlipType rotateFlip = RotateFlipType.RotateNoneFlipNone, PixelFormat format = default, Color backColor = default(Color), int width = 0, int height = 0, int column = 0, int row = 0, bool horizontalFirst = true, bool mainReverse = false, bool crossReverse = false)
        {
            DealState dealState;
            int progress, progressEnd;

            List<Image> frames = new List<Image>();
            Bitmap totalBmp;
            Graphics bmpG;

            try
            {
                dealState = 0;           // 处理参数
                progress = 0;
                progressEnd = 0;
                progressReporter.Invoke(dealState, progress, progressEnd);
                if (images.Length > 0 && width >= 0 && height >= 0 && column >= 0 && Enum.IsDefined(typeof(ImageSizingMode), sizingMode))
                {
                    dealState = DealState.ProcessLoadImages;       // 打开图像
                    progress = 0;
                    progressEnd = images.Length;
                    try
                    {
                        foreach (string i in images)
                        {
                            Image inputImg = Image.FromFile(i);
                            inputImg.RotateFlip(rotateFlip);
                            frames.Add(inputImg);
                            progress++;
                            progressReporter.Invoke(dealState, progress, progressEnd);
                        }
                    }
                    catch
                    {
                        result = null;
                        return -2;
                    }

                    dealState = DealState.ProcessImageSizes;         // 处理图像尺寸
                    progress = -1;
                    progressEnd = -1;
                    progressReporter.Invoke(dealState, progress, progressEnd);
                    if (width == 0)
                    {
                        int maxWidth = 0;
                        foreach (Image i in frames)
                        {
                            if (i.Width > maxWidth)
                            {
                                maxWidth = i.Width;
                            }
                        }
                        width = maxWidth;
                    }
                    if (height == 0)
                    {
                        int maxHeight = 0;
                        foreach (Image i in frames)
                        {
                            if (i.Height > maxHeight)
                            {
                                maxHeight = i.Height;
                            }
                        }
                        height = maxHeight;
                    }
                    if (column == 0)
                    {
                        if (row == 0)
                            column = (int)Math.Pow(frames.Count, 0.5);
                        else
                            column = (int)Math.Ceiling((double)frames.Count / row);
                    }

                    if (row == 0)
                        row = (int)Math.Ceiling((double)frames.Count / column);


                    dealState = DealState.ProcessDrawing;            // 正式拼合
                    progress = 0;
                    progressEnd = frames.Count;
                    progressReporter.Invoke(dealState, progress, progressEnd);
                    if (format == default)
                    {
                        totalBmp = new Bitmap(width * column, height * row, frames[0].PixelFormat);
                    }
                    else
                    {
                        totalBmp = new Bitmap(width * column, height * row, format);
                    }
                    totalBmp.MakeTransparent();
                    totalBmp.SetResolution(frames[0].HorizontalResolution, frames[0].VerticalResolution);

                    bmpG = Graphics.FromImage(totalBmp);
                    bmpG.Clear(backColor);

                    if (horizontalFirst)
                    {
                        if (!crossReverse)
                        {
                            if (!mainReverse)
                            {
                                for (int i = 0; i < frames.Count; i++)
                                {
                                    for (int j = 0; j < column && i * column + j < frames.Count; j++)
                                    {
                                        //bmgG.DrawImage(frames[i * columns + j], new Point(j * width, i * height));
                                        Image imgToDraw = frames[i * column + j];
                                        Point offset = new Point(j * width, i * height);
                                        Rectangle destRect = CalcImageAbsBounds(sizingMode, width, height, imgToDraw.Width, imgToDraw.Height);
                                        destRect.Offset(offset);
                                        bmpG.SetClip(new Rectangle(offset.X, offset.Y, width, height));
                                        bmpG.DrawImage(imgToDraw, destRect);
                                        progress++;
                                        progressReporter.Invoke(dealState, progress, progressEnd);
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < frames.Count; i++)
                                {
                                    for (int j = 0; j < column && i * column + j < frames.Count; j++)
                                    {
                                        //bmgG.DrawImage(frames[i * columns + j], new Point((columns - j - 1) * width, i * height));
                                        Image imgToDraw = frames[i * column + j];
                                        Point offset = new Point((column - j - 1) * width, i * height);
                                        Rectangle destRect = CalcImageAbsBounds(sizingMode, width, height, imgToDraw.Width, imgToDraw.Height);
                                        destRect.Offset(offset);
                                        bmpG.SetClip(new Rectangle(offset.X, offset.Y, width, height));
                                        bmpG.DrawImage(imgToDraw, destRect);
                                        progress++;
                                        progressReporter.Invoke(dealState, progress, progressEnd);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!mainReverse)
                            {
                                for (int i = 0; i < frames.Count; i++)
                                {
                                    for (int j = 0; j < column && i * column + j < frames.Count; j++)
                                    {
                                        //bmgG.DrawImage(frames[i * columns + j], new Point(j * width, (rows - i - 1) * height));
                                        Image imgToDraw = frames[i * column + j];
                                        Point offset = new Point(j * width, (row - i - 1) * height);
                                        Rectangle destRect = CalcImageAbsBounds(sizingMode, width, height, imgToDraw.Width, imgToDraw.Height);
                                        destRect.Offset(offset);
                                        bmpG.SetClip(new Rectangle(offset.X, offset.Y, width, height));
                                        bmpG.DrawImage(imgToDraw, destRect);
                                        progress++;
                                        progressReporter.Invoke(dealState, progress, progressEnd);
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < frames.Count; i++)
                                {
                                    for (int j = 0; j < column; j++)
                                    {
                                        //bmgG.DrawImage(frames[i * columns + j], new Point((columns - j) * width, (rows - i - 1) * height));
                                        Image imgToDraw = frames[i * column + j];
                                        Point offset = new Point((column - j) * width, (row - i - 1) *height);
                                        Rectangle destRect = CalcImageAbsBounds(sizingMode, width, height, imgToDraw.Width, imgToDraw.Height);
                                        destRect.Offset(offset);
                                        bmpG.SetClip(new Rectangle(offset.X, offset.Y, width, height));
                                        bmpG.DrawImage(imgToDraw, destRect);
                                        progress++;
                                        progressReporter.Invoke(dealState, progress, progressEnd);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!crossReverse)
                        {
                            if (!mainReverse)
                            {
                                for (int i = 0; i < column; i++) // column 列数
                                {
                                    for (int j = 0; j < row && i * row + j < frames.Count; j++)
                                    {
                                        //bmgG.DrawImage(frames[i * rows + j], new Point(i * width, j * height));
                                        Image imgToDraw = frames[i * column + j];
                                        Point offset = new Point(i * width, j * height);
                                        Rectangle destRect = CalcImageAbsBounds(sizingMode, width, height, imgToDraw.Width, imgToDraw.Height);
                                        destRect.Offset(offset);
                                        bmpG.SetClip(new Rectangle(offset.X, offset.Y, width, height));
                                        bmpG.DrawImage(imgToDraw, destRect);
                                        progress++;
                                        progressReporter.Invoke(dealState, progress, progressEnd);
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < column; i++)
                                {
                                    for (int j = 0; j < row && i * column + j < frames.Count; j++)
                                    {
                                        bmpG.DrawImage(frames[i * column + j], new Point(i * width, (row - j - 1) * height));
                                        Image imgToDraw = frames[i * column + j];
                                        Point offset = new Point(i * width, (row - j - 1) * height);
                                        Rectangle destRect = CalcImageAbsBounds(sizingMode, width, height, imgToDraw.Width, imgToDraw.Height);
                                        destRect.Offset(offset);
                                        bmpG.SetClip(new Rectangle(offset.X, offset.Y, width, height));
                                        bmpG.DrawImage(imgToDraw, destRect);
                                        progress++;
                                        progressReporter.Invoke(dealState, progress, progressEnd);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!mainReverse)
                            {
                                for (int i = 0; i < column; i++)
                                {
                                    for (int j = 0; j < row && i * row + j < frames.Count; j++)
                                    {
                                        //bmgG.DrawImage(frames[i * rows + j], new Point((columns - i - 1) * width, j * height));
                                        Image imgToDraw = frames[i * column + j];
                                        Point offset = new Point((column - i - 1) * width, j * height);
                                        Rectangle destRect = CalcImageAbsBounds(sizingMode, width, height, imgToDraw.Width, imgToDraw.Height);
                                        destRect.Offset(offset);
                                        bmpG.SetClip(new Rectangle(offset.X, offset.Y, width, height));
                                        bmpG.DrawImage(imgToDraw, destRect);
                                        progress++;
                                        progressReporter.Invoke(dealState, progress, progressEnd);
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < column; i++)
                                {
                                    for (int j = 0; j < row && i * row + j < frames.Count; j++)
                                    {
                                        //bmgG.DrawImage(frames[i * rows + j], new Point((columns - i - 1) * width, (rows - j - 1) * height));
                                        Image imgToDraw = frames[i * column + j];
                                        Point offset = new Point((column - i - 1) * width, (row - j - 1) * height);
                                        Rectangle destRect = CalcImageAbsBounds(sizingMode, width, height, imgToDraw.Width, imgToDraw.Height);
                                        destRect.Offset(offset);
                                        bmpG.SetClip(new Rectangle(offset.X, offset.Y, width, height));
                                        bmpG.DrawImage(imgToDraw, destRect);
                                        progress++;
                                        progressReporter.Invoke(dealState, progress, progressEnd);
                                    }
                                }
                            }
                        }
                    }
                    dealState = DealState.ProcessReturn;        // 返回结果
                    progress = -1;
                    progressEnd = -1;
                    progressReporter.Invoke(dealState, progress, progressEnd);
                    bmpG.Save();
                    try
                    {
                        result = totalBmp;
                        return 0;
                    }
                    catch
                    {
                        result = null;
                        return -4;
                    }
                }
                else
                {
                    result = null;
                    return -1;
                }
            }
            finally
            {
                foreach (Image item in frames)
                {
                    if (item != null)
                    {
                        item.Dispose();
                    }
                }
            }
        }
        public static int Combine(string[] images, Action<DealState, int, int> progressReporter, string resultPath, ImageSizingMode sizingMode = 0, RotateFlipType rotateFlip = RotateFlipType.RotateNoneFlipNone, PixelFormat format = default, Color backColor = default(Color), int width = 0, int height = 0, int column = 0, int row = 0, bool horizontalFirst = true, bool mainReverse = false, bool crossReverse = false)
        {
            int returnCode = Combine(images, progressReporter, out Bitmap rst, sizingMode, rotateFlip, format, backColor, width, height, column, row, horizontalFirst, mainReverse, crossReverse);
            if (returnCode == 0)
            {
                progressReporter.Invoke(DealState.ProcessSave, -1, -1);
                try
                {
                    rst.Save(resultPath);
                    return returnCode;
                }
                catch
                {
                    return -5;    // 保存失败
                }
            }
            else
            {
                return returnCode;
            }
        }
    }
}
