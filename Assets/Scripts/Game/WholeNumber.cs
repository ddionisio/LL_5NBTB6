using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct WholeNumber {
    public static int TenExponent(int exp) {
        if(exp < 0)
            return 0;

        int ret = 1;
        for(int i = 0; i < exp; i++)
            ret *= 10;

        return ret;
    }

    /// <summary>
    /// Grab the digit from number, count from right to left with 0 being the first digit.
    /// </summary>
    public static void ExtractDigit(int number, int digitCount, out int newNumber, out int digitNumber) {
        digitNumber = number;

        if(digitCount > 0) {
            var digitShift = TenExponent(digitCount);

            digitNumber /= digitShift;

            digitNumber %= 10;

            digitNumber *= digitShift;
        }
        else
            digitNumber %= 10;

        newNumber = number - digitNumber;
    }

    public static int DigitCount(int number) {
        int count = 0;
        while(number > 0) {
            number /= 10;
            count++;
        }

        return count;
    }

    public static int NonZeroDigitCount(int number) {
        int count = 0;

        while(number > 0) {
            if(number % 10 != 0)
                count++;

            number /= 10;
        }

        return count;
    }

    public static bool GetFirstNonZeroDigit(int number, out int digitValue, out int digitIndex) {
        digitValue = 0;
        digitIndex = 0;

		while(number > 0) {
			digitValue = number % 10;
            if(digitValue != 0)
                return true;

			number /= 10;
            digitIndex++;
		}

		return false;
	}

    public static int ZeroCount(int number) {
        int count = 0;
        for(int numStep = number; numStep % 10 == 0; count++)
            numStep /= 10;

        return count;
    }

    public static int NonZeroCount(int number) {
        int count = 0;

        for(int numStep = number; numStep > 0; numStep /= 10) {
            if(numStep % 10 != 0)
                count++;
        }

        return count;
    }

    public static string RepeatingChar(int number, char ch) {
        var digitCount = DigitCount(number);

        var sb = new System.Text.StringBuilder(digitCount);
        for(int i = 0; i < digitCount; i++)
            sb.Append(ch);

        return sb.ToString();
    }
}
