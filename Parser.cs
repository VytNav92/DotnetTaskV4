using DotnetTaskV4.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DotnetTaskV4
{
    public static class Parser
    {
        private const int BytesToSkip = 4200;
        private const string DataDelimiter = ", ";
        private const int DelimitersCount = 6;
        private const int MinimumRentLength = 64;

        private static readonly char CheckResultFailLastCharacter = DeviceData.CheckResultFail[DeviceData.CheckResultFail.Length - 1];
        private static readonly char CheckResultPassLastCharacter = DeviceData.CheckResultPass[DeviceData.CheckResultPass.Length - 1];

        public static async IAsyncEnumerable<DeviceData> ParseAsync(StreamReader reader, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            reader.BaseStream.Seek(BytesToSkip, SeekOrigin.Current);

            var delimiterEndPositions = new int[DelimitersCount];
            int bufferLastIndex, delimitersCount = 0, lineStartIndex, remainderSize = 0;
            bool canBeDelimiter = false, canBeEndLine = false;
            char[] currentBuffer, preparedBufferWithRemainder = ArrayPool<char>.Shared.Rent(MinimumRentLength);

            while (!reader.EndOfStream)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                lineStartIndex = 0;
                currentBuffer = preparedBufferWithRemainder;
                await reader.ReadAsync(currentBuffer, remainderSize, MinimumRentLength);

                bufferLastIndex = currentBuffer.Length;

                for (int i = remainderSize; i < currentBuffer.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        yield break;

                    if (IsEndOfStream(currentBuffer[i]))
                    {
                        bufferLastIndex = i;
                        break;
                    }

                    if (canBeDelimiter)
                    {
                        if (IsDelimiterEnd(currentBuffer[i]))
                        {
                            delimiterEndPositions[delimitersCount] = i;
                            delimitersCount++;
                        }

                        canBeDelimiter = false;
                        continue;
                    }

                    if (IsDelimiterBeginning(currentBuffer[i]))
                    {
                        canBeDelimiter = true;
                        continue;
                    }

                    if (currentBuffer[i] == '\r')
                    {
                        canBeEndLine = true;
                        continue;
                    }

                    if (canBeEndLine)
                    {
                        canBeEndLine = false;
                        if (currentBuffer[i] == '\n')
                        {
                            if (delimitersCount == DelimitersCount && IsDataRowEnd(currentBuffer[i - 2]))
                                yield return ParseData(currentBuffer, lineStartIndex, i - 2, delimiterEndPositions);

                            lineStartIndex = i + 1;
                            delimitersCount = 0;
                            continue;
                        }
                    }
                }


                remainderSize = bufferLastIndex - lineStartIndex;

                preparedBufferWithRemainder = ArrayPool<char>.Shared.Rent(bufferLastIndex - lineStartIndex + MinimumRentLength);
                Array.Copy(currentBuffer, lineStartIndex, preparedBufferWithRemainder, 0, remainderSize);
                ArrayPool<char>.Shared.Return(currentBuffer, true);
                for (int i = 0; i < delimitersCount; i++)
                    delimiterEndPositions[i] = delimiterEndPositions[i] - lineStartIndex;
            }

            ArrayPool<char>.Shared.Return(preparedBufferWithRemainder, true);
        }

        private static bool IsEndOfStream(char character)
        {
            return character == '\0';
        }

        private static bool IsDelimiterBeginning(char character)
        {
            return character == DataDelimiter[0];
        }

        private static bool IsDelimiterEnd(char character)
        {
            return character == DataDelimiter[1];
        }

        private static bool IsDataRowEnd(char character)
        {
            return character == CheckResultFailLastCharacter || character == CheckResultPassLastCharacter;
        }

        private static DeviceData ParseData(char[] buffer, int startIndex, int endIndex, int[] delimiterEndPositions)
        {
            var bandSpan = buffer.AsSpan(startIndex, delimiterEndPositions[0] - 1 - startIndex);
            var plcSpan = GetSpan(buffer, delimiterEndPositions[0], delimiterEndPositions[1]);
            var txPowerSpan = GetSpan(buffer, delimiterEndPositions[1], delimiterEndPositions[2]);
            var targetPowerSpan = GetSpan(buffer, delimiterEndPositions[2], delimiterEndPositions[3]);
            var minPowerSpan = GetSpan(buffer, delimiterEndPositions[3], delimiterEndPositions[4]);
            var maxPowerSpan = GetSpan(buffer, delimiterEndPositions[4], delimiterEndPositions[5]);
            var checkResultSpan = buffer.AsSpan(delimiterEndPositions[5] + 1, endIndex - delimiterEndPositions[5]);

            return new DeviceData
            {
                Band = bandSpan.ToString(),
                Plc = int.Parse(plcSpan),
                TxPower = float.Parse(txPowerSpan),
                TargetPower = float.Parse(targetPowerSpan),
                MinPower = float.Parse(minPowerSpan),
                MaxPower = float.Parse(maxPowerSpan),
                CheckResult = checkResultSpan.SequenceCompareTo(DeviceData.CheckResultPass) == 0
            };
        }

        private static Span<char> GetSpan(char[] buffer, int previousDelimiterPosition, int nextDelimiterPosition)
        {
            return buffer.AsSpan(previousDelimiterPosition + 1, nextDelimiterPosition - previousDelimiterPosition - DataDelimiter.Length);
        }
    }
}
