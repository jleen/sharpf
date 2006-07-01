/*
 * bignum.cs:
 *
 * Bignum package.  Supports very large integers (largely limited by
 * efficiency and memory usage).
 *
 * Lots of this code is really cheesy and could really use some cleanup,
 * but it implements a lot of the basic functionality that a Scheme
 * implementation needs.
 *
 */

using System;
using System.Diagnostics;
using System.Text;

namespace SaturnValley.SharpF
{
    public class BigNum : Number, IComparable<int>, IComparable<BigNum>
    {
        // The representation here is an array of integers, each
        // representing a single digit in a given large base.
        // I'm using an explicit sign bit instead of a complement
        // representation, which makes some things easier.
        private int[] digits = new int[] { 0 };
        private bool isNegative = false;
        private int size = 0;

        // REVIEW (RandyTh) 32768 would be more efficient, but
        // this is handy for debugging.
        
        const int digitBase = 10000;

        public BigNum()
        {
        }

        public BigNum(BigNum bn)
        {
            Assign(bn);
        }

        public BigNum(int i)
        {
            Assign(i);
        }

        // In a lot of this file, you'll see how much I'm missing operator
        // overloading.
        //
        // REVIEW (RandyTh): In many of the routines that take primitive
        // integers, I have not exhaustively tested them in cases where
        // the integer is of larger magnitude than the digitBase.  Bugs
        // here are certainly possible -- in most cases, I'd probably just
        // upconvert to a full BigNum to handle them.
        
        public void Assign(int iVal)
        {
            int i = 0;
            isNegative = i < 0;
            
            // REVIEW (RandyTh): I'm doing the negation digit by digit
            // instead of up front in an attempt to avoid a potential bug
            // where negating the largest possible integer could overflow.
            
            while (iVal != 0)
            {
                // REVIEW PERF (RandyTh): It would be more efficient to
                // ensure the proper array size up front instead of
                // calling the slower routine that extends one at a time.
                // We use exponential growth, so it shouldn't be too bad,
                // and I'm also hoping that assignment of fixnums to
                // bignums is not a perf critical codepath.

                // REVIEW PERF (RandyTh): I'm doing the simple thing here
                // of calling both mod and division, but this could be
                // optimized in cases where integer division is slow
                // compared to integer multiplication and addition.
                
                SetDigit(i, iVal % digitBase);
                if (isNegative)
                {
                    digits[i] = -digits[i];
                }
                iVal /= digitBase;
                ++i;
            }
            Normalize();
        }

        public void Assign(BigNum bn)
        {
            // REVIEW PERF (RandyTh): this could also be optimized if I
            // were willing to duplicate a little more code and trust
            // incoming BigNum objects more.
            
            size = bn.size;
            EnsureIndex(bn.size);
            Array.Copy(bn.digits, digits, size);
            isNegative = bn.isNegative;
        }

        // property accessors
        public bool IsNegative
        {
            get
            {
                return isNegative;
            }
        }

        public int Size
        {
            get
            {
                return size;
            }
        }

        // do a bounds check to see if this is within the range
        // for a single digit in our representation.
        //
        // REVIEW (RandyTh): this isn't the best name for this method IMO.
        public static bool IsLegalFixNum(int iNum)
        {
            return (iNum < digitBase && iNum > -digitBase);
        }

        // comparison operators
        public int CompareTo(BigNum other)
        {
            // REVIEW PERF (RandyTh): I'm not confident that I've fully
            // optimized the early exits in this routine based on actual
            // frequencies.  For example -- is it worth putting the zero
            // check higher up in the function?
            
            // First, handle the easy case of sign mismatch.
            if (isNegative && !other.isNegative)
            {
                return -1;
            }
            else if (!isNegative && other.isNegative)
            {
                return 1;
            }
            else
            {
                // Next, handle the case where the two numbers differ
                // in magnitude.
                Debug.Assert(isNegative == other.isNegative);
                if (size != other.size)
                {
                    return (isNegative ?
                            other.size - size :
                            size - other.size);
                }
                else if (size == 0)
                {
                    // both numbers are 0
                    return 0;
                }
                else
                {
                    // Now we know that the numbers have the same magnitude,
                    // so we do the full comparison digit by digit.
                    Debug.Assert(size == other.size);
                    for (int i = size - 1; i >= 0; --i)
                    {
                        if (digits[i] != other.digits[i])
                        {
                            return (isNegative ?
                                other.digits[i] - digits[i] :
                                digits[i] - other.digits[i]);
                        }
                    }
                    return 0;
                }
            }
        }

        // This is directly analogous to the bignum case, but useful for
        // comparison with fixnums without boxing in the common case.
        public int CompareTo(int other)
        {
            bool otherIsNegative = (other < 0);
            if (size == 0 && other == 0)
            {
                return 0;
            }
            else if (isNegative && !otherIsNegative)
            {
                return -1;
            }
            else if (!isNegative && otherIsNegative)
            {
                return 1;
            }
            else
            {
                Debug.Assert(isNegative == otherIsNegative);

                int otherSize = 0;
                int otherTemp = other;
                while (otherTemp != 0)
                {
                    ++otherSize;
                    otherTemp /= digitBase;
                }
                if (size != otherSize)
                {
                    return (isNegative ?
                            otherSize - size :
                            size - otherSize);
                }
                else if (size == otherSize && size <= 1)
                {
                    return (isNegative ? 
                        other - GetDigit(0) : 
                        GetDigit(0) - other); 
                }
                else
                {
                    // handle the case of "big" non-BigNums by conversion
                    // REVIEW (RandyTh): this is obviously non optimal in
                    // efficiency, but I hope this case is fairly rare.

                    BigNum bnTemp = new BigNum(other);
                    return CompareTo(other);
                }
            }
        }

        // absolute value comparison
        public int CompareToAbs(BigNum other)
        {
            if (size != other.size)
            {
                return size - other.size;
            }
            else if (size == 0)
            {
                return 0;
            }
            else
            {
                for (int i = size - 1; i >= 0; --i)
                {
                    if (digits[i] != other.digits[i])
                    {
                        return Math.Abs(digits[i]) -
                            Math.Abs(other.digits[i]);
                    }
                }
                return 0;
            }
        }

        public int CompareToAbs(int other)
        {
            bool otherIsNegative = (other < 0);

            // If this int is out of range of our radix, there's
            // a chance that these comparisons could overflow.
            if (IsLegalFixNum(other))
            {
                if (isNegative && !otherIsNegative)
                {
                    return -CompareTo(-other);
                }
                else if (!isNegative && otherIsNegative)
                {
                    return CompareTo(-other);
                }
                else
                {
                    return isNegative ? -CompareTo(other) : CompareTo(other);
                }
            }
            else
            {
                return CompareToAbs(new BigNum(other));
            }
        }


        public static BigNum Add(BigNum a, BigNum b)
        {
            BigNum result;

            // if we have mixed sign results, convert this to
            // subtraction of the addend with higher absolute value
            // from the one with lower absolute value, then fix the
            // sign of the result accordingly.
            if (a.isNegative != b.isNegative)
            {
                int iAbsComp = a.CompareToAbs(b);
                if (iAbsComp > 0)
                {
                    if (a.isNegative)
                    {
                        result = SubInternal(a, b);
                        result.isNegative = true;
                    }
                    else
                    {
                        result = SubInternal(a, b);
                        result.isNegative = false;
                    }
                }
                else if (iAbsComp < 0)
                {
                    if (a.isNegative)
                    {
                        result = SubInternal(b, a);
                        result.isNegative = false;
                    }
                    else
                    {
                        result = SubInternal(b, a);
                        result.isNegative = true;
                    }
                }
                else
                {
                    result = new BigNum();

                }
            }
            else
            {
                // handle the matched sign case
                Debug.Assert(a.isNegative == b.isNegative);
                result = AddInternal(a, b);
                result.isNegative = a.isNegative;
            }
            
            return result;
        }

        // NOTE: this does unsigned addition of these two BigNums.
        // If you use this for mixed sign values, make sure you know
        // what you are doing.
        private static BigNum AddInternal(BigNum a, BigNum b)
        {
            int n = Math.Max(a.size, b.size);
            int nMin = Math.Min(a.size, b.size);
            BigNum result = new BigNum();
            result.EnsureIndex(n + 1);

            int j, k;
            k = 0;

            // First, handle the range where both BigNums are nonzero.
            for (j = 0; j < nMin; ++j)
            {
                result.digits[j] = a.digits[j] + b.digits[j] + k;
                k = 0;
                while (result.digits[j] > digitBase)
                {
                    result.digits[j] -= digitBase;
                    ++k;
                }
            }

            // Finish up with the range where one BigNum may be zero
            // using the slower GetDigit primitive that zero-extends
            // the numbers.
            for (j = nMin; j < n; ++j)
            {
                result.digits[j] = a.GetDigit(j) + b.GetDigit(j) + k;
                k = 0;
                while (result.digits[j] > digitBase)
                {
                    result.digits[j] -= digitBase;
                    ++k;
                }
            }

            // handle any final carry.
            if (k != 0)
            {
                result.digits[n] = k;
                j = n;
            }

            // calculate the resulting sign and magnitude.
            result.isNegative = a.isNegative;
            result.size = 0;

            for (; j >= 0; --j)
            {
                if (result.digits[j] != 0)
                {
                    result.size = j + 1;
                    break;
                }
            }

            return result;
        }

        public static BigNum Sub(BigNum a, BigNum b)
        {
            // Again, we reduce this to an unsigned operation, either
            // adding two numbers or subtracting a number of lesser
            // absolute magnitude from another of larger absolute
            // magnitude.
            BigNum result;
            int iAbsComp = a.CompareToAbs(b);
            if (a.isNegative != b.isNegative)
            {
                if (a.isNegative)
                {
                    result = AddInternal(a, b);
                    result.isNegative = true;
                }
                else
                {
                    result = AddInternal(a, b);
                    result.isNegative = false;
                }
            }
            else
            {
                if (iAbsComp > 0)
                {
                    result = SubInternal(a, b);
                    result.isNegative = a.isNegative;
                }
                else if (iAbsComp < 0)
                {
                    result = SubInternal(b, a);
                    result.isNegative = !a.isNegative;
                }
                else
                {
                    result = new BigNum();
                }
            }

            return result;
        }
        
        // This performs unsigned arithmetic, and assumes that
        // a is greater in absolute magnitude than b.
        public static BigNum SubInternal(BigNum a, BigNum b)
        {
            Debug.Assert(a.CompareToAbs(b) >= 0);

            int n = Math.Max(a.size, b.size);
            int nMin = Math.Min(a.size, b.size);
            BigNum result = new BigNum();
            result.EnsureIndex(n);

            int j, k;
            k = 0;
            // First, handle the range where both BigNums are nonzero.
            for (j = 0; j < nMin; ++j)
            {            
                result.digits[j] = a.digits[j] - b.digits[j] + k;
                k = 0;
                while (result.digits[j] < 0)
                {
                    result.digits[j] += digitBase;
                    --k;
                }
            }

            // Finish up with the range where one BigNum may be zero
            // using the slower GetDigit primitive that zero-extends
            // the numbers.            
            for (j = nMin; j < n; ++j)
            {            
                result.digits[j] = a.GetDigit(j) - b.GetDigit(j) + k;
                k = 0;
                while (result.digits[j] < 0)
                {
                    result.digits[j] += digitBase;
                    --k;
                }
            }

            result.size = 0;

            for (; j >= 0; --j)
            {
                if (result.digits[j] != 0)
                {
                    result.size = j + 1;
                    break;
                }
            }

            return result;
        }

        // simple cases intended for accumulation or initialization
        public void Add(BigNum a)
        {
            if (isNegative == a.isNegative)
            {
                EnsureIndex(a.size);

                int i = 0;
                for (i = 0; i < a.size; ++i)
                {
                    digits[i] += a.digits[i] ;
                }
                Normalize();
            }
            else
            {
                this.Assign(Add(this, a));
            }
        }

        public void Add(int iVal)
        {
            bool iValIsNegative = (iVal < 0);

            if (IsLegalFixNum(iVal) &&
                (size == 0 || iValIsNegative == isNegative))
            {
                SetDigit(0, GetDigit(0) +  (iValIsNegative ? -iVal : iVal));
                Normalize();
            }
            else
            {
                Add(new BigNum(iVal));
            }
        }

        // flip the sign bit
        public void Negate()
        {
            // Note that all zeros are equal to each other, and
            // we try to standardize on positive zero.
            //
            // REVIEW (RandyTh): should we try to assert this in more
            // places?
            
            if (size > 0)
            {
                isNegative = !isNegative;
            }
        }

        // Divide a BigNum by a fixed number.
        public void ShortDiv(int iDiv, out int iRem)
        {
            int i;
            iRem = 0;

            // this routine is not in general correct for all primitive
            // integers, so I'm limiting this to numbers that would take
            // up a single digit in our representation.
            Debug.Assert(IsLegalFixNum(iDiv));

            if (iDiv == 0)
            {
                throw new MathException("Division by zero!");
            }

            // Note that this step would not be correct if iDiv
            // was the largest possible primitive integer.
            if (iDiv < 0)
            {
                iDiv = -iDiv;
                Negate();
            }

            // do a simple rippling placewise division.
            for (i = size - 1; i >= 0; --i)
            {
                digits[i] = Math.DivRem(
                    digits[i] + iRem * digitBase,
                    iDiv, out iRem);
            }

            // clean up the result array.
            Normalize();
        }

        // wrapper routines for short division
        public static BigNum ShortDiv(BigNum a, int iDiv, out int iRem)
        {
            BigNum result = new BigNum(a);
            result.ShortDiv(iDiv, out iRem);

            return result;
        }

        public static BigNum ShortDiv(BigNum a, int iDiv)
        {
            int iRem = 0;
            return ShortDiv(a, iDiv, out iRem);
        }

        // Multiply a BigNum by a fixed number.
        public void ShortMul(int iMul)
        {
            int i;

            Debug.Assert(IsLegalFixNum(iMul));
            
            // this should look fairly similar as compared to
            // the ShortDiv case.
            if (iMul < 0)
            {
                iMul = -iMul;
                Negate();
            }

            for (i = 0; i < size; ++i)
            {
                digits[i] *= iMul;
            }

            isNegative ^= (iMul < 0);
            Normalize();
        }

        public static BigNum ShortMul(BigNum bn, int iMul)
        {
            BigNum result = new BigNum(bn);
            result.ShortMul(iMul);

            return result;
        }

        public static BigNum ShortMul(int iMul, BigNum bn)
        {
            BigNum result = new BigNum(bn);
            result.ShortMul(iMul);

            return result;
        }

        // multiply by (baseDigit^iDigits) through a shift
        public void ShiftMul(int iDigits)
        {
            EnsureIndex(size + iDigits);
            Array.Copy(digits, 0, digits, iDigits, size);
            size += iDigits;
        }

        // divide by (baseDigit^iDigits) through a shift
        public void ShiftDiv(int iDigits)
        {
            if (iDigits < size)
            {
                Array.Copy(digits, iDigits, digits, 0, size);
                size -= iDigits;
            }
            else
            {
                Zero();
            }
        }

        // Multiply two BigNums.
        public static BigNum LongMul(BigNum a, BigNum b)
        {
            int i, j;
            BigNum bnResult = new BigNum();
            bnResult.EnsureIndex(a.size + b.size);
            bnResult.Zero();

            // REVIEW PERF (RandyTh): This uses the simple naive O(NM) or
            // O(N^2) algorithm.  It would be better if I switched to an
            // asymptotically efficient algorithm for very large bignums.
            // A Karatsuba or Toom-Cook algorithm (recursive
            // divide-and-conquer) would be the simplest approach to lower
            // the exponenent in the polynomial here, but a modular FFT
            // algorithm like Schoenhage-Strassen would be more efficient
            // at O(N*log(N)*log(log(N))).
            //
            // Note however, that the constant on the simple approach
            // is quite low, so it wins until the bignums get to be
            // fairly large.
            
            for (i = 0; i < a.size; ++i)
            {
                for (j = 0; j < b.size; ++j)
                {
                    bnResult.digits[i + j] += a.digits[i] * b.digits[j];
                }

                // REVIEW PERF (RandyTh): Using Normalize here is lazy and
                // cheesy.  Directly handling the rippling overflow is
                // more efficient in the common case of only propagating
                // to a few digits, but Normalize is always pessimistic.
                
                bnResult.Normalize();
            }

            bnResult.isNegative = a.isNegative ^ b.isNegative;
            return bnResult;
        }
        
        // Divide one BigNum by another, with remainder.
        // This is a lame implementation of the Algorithm D in
        // Knuth TAOCP 3rd ed, section 4.3.1.
        public static BigNum LongDiv(
            BigNum bnNum,
            BigNum bnDiv,
            out BigNum bnRem)
        {
            // handle the zero cases first
            int m = bnNum.size;
            int n = bnDiv.size;

            BigNum bnResult = new BigNum();
            bnRem = new BigNum();

            if (n == 0)
                throw new MathException("Division by zero!");

            bool bResultIsNegative = (bnNum.isNegative ^ bnDiv.isNegative);

            // zero divided by anything is zero.
            if (m == 0)
            {
                return bnResult;
            }

            // Note: this is not just for efficiency -- the main loop does
            // not handle single digit BigNums correctly.
            if (n == 1)
            {
                int iRem = 0;
                bnResult = new BigNum(bnNum);
                bnResult.ShortDiv(bnDiv.digits[0], out iRem);
                bnRem.Add(iRem);
                return bnResult;
            }

            // If we're dividing a number by one that has a greater
            // absolute magnitude, the result is zero and the remainder is
            // the original numerator.
            
            if (n > m)
            {
                bnRem = new BigNum(bnNum);
                return bnResult;
            }

            // allocate enough space to work with.
            bnResult.EnsureIndex(m - n + 1);
            int d = digitBase / (bnDiv.digits[n - 1] + 1);
            bnNum = new BigNum(bnNum);
            bnDiv = new BigNum(bnDiv);
            BigNum bnTemp = new BigNum();

            // We scale up the numerator and denominator so that our
            // estimates on quotient digits will be accurate nearly all of
            // the time.
            bnNum.ShortMul(d);
            bnDiv.ShortMul(d);

            int j;
            int iDummy;

            for (j = m - n + 1; j >= 0; --j)
            {
                // estimate the current digit of the result with a trial
                // division of the leading two digits of the numerator by
                // the leading digit of the denominator.
                int rhat = 0;
                int qhat = Math.DivRem(
                    bnNum.GetDigit(j + n) * digitBase +
                    bnNum.GetDigit(j + n - 1),
                    bnDiv.digits[n - 1], out rhat);

                // Refine our guess, if needed.  This should be quite
                // rare.
                // REVIEW (RandyTh): That statement almost certainly
                // means there's an error in this case.
                while ((qhat == digitBase) ||
                       ((qhat * bnDiv.digits[n - 2]) >
                        (digitBase * rhat + bnNum.digits[j + n - 2])))
                {
                    --qhat;
                    rhat += bnDiv.digits[n - 1];
                    if (rhat >= digitBase)
                        break;
                }

                // store the digit of the result.
                bnResult.digits[j] = qhat;

                // if this digit is nonzero, multiply and subtract.
                // REVIEW PERF (RandyTh): this implementation is really
                // cheesy, but it is convenient.  Really, at some point I
                // should do the heavy lifting and properly do the
                // shifting in place and not rely on the external
                // routines.  This is much less efficient than it should
                // be on large integers because of these shortcuts.

                if (qhat != 0)
                {
                    bnTemp.Assign(bnDiv);
                    bnTemp.ShortMul(qhat);
                    while (bnTemp.CompareToAbs(bnNum) < 0)
                    {
                        bnTemp.ShiftMul(1);
                    }

                    if (bnTemp.CompareToAbs(bnNum) > 0)
                    {
                        bnTemp.ShiftDiv(1);
                    }

                    BigNum bnTemp2 = BigNum.SubInternal(bnNum, bnTemp);

                    // if we underflowed, add back the last multiple.
                    if (bnNum.CompareToAbs(bnTemp) < 0)
                    {
                        --bnResult.digits[j];
                        bnTemp.ShortDiv(qhat, out iDummy);
                        bnTemp.ShortMul(qhat - 1);
                        bnNum = BigNum.AddInternal(bnTemp2, bnDiv);
                    }
                    else
                    {
                        bnNum = bnTemp2;
                    }
                }
            }

            // renormalize the numerator to calculate our remainder.
            bnNum.ShortDiv(d, out iDummy);
            bnRem = bnNum;
            bnResult.Normalize();
            bnResult.isNegative = bResultIsNegative;

            return bnResult;
        }

        // wrapper for those that don't need the remainder.
        // REVIEW PERF (RandyTh): should I optimize further here, e.g.
        // actually suppressing the remainder calculation?
        public static BigNum LongDiv(BigNum a, BigNum b)
        {
            BigNum dummy;
            return LongDiv(a, b, out dummy);         
        }

        public static BigNum Remainder(BigNum a, BigNum b)
        {
            BigNum rem;
            LongDiv(a, b, out rem);
            return rem;
        }

        // simple modern Euclidean algorithm reduction.
        //
        // REVIEW PERF (RandyTh): If my division algorithm is reasonably
        // efficient, this is a good asymptotic algorithm when the numbers
        // of widely differing magnitudes.  Once the numbers are close in
        // magnitude, iterated subtraction is probably faster again.
        // However, this shouldn't have too many highly degenerate cases,
        // so this is probably OK for now.
        public static BigNum Gcd(BigNum u, BigNum v)
        {
            if (v.CompareTo(1) == 0 || u.CompareTo(1) == 0)
                return new BigNum(1);
    
            u = new BigNum(u);
            v = new BigNum(v);

            if (u.isNegative)
            {
                u.Negate();
            }
            
            if (v.IsNegative)
            {
                v.Negate();
            }
            
            BigNum r = null;
            while (v.Size != 0)
            {
                BigNum.LongDiv(u, v, out r);
                u.Assign(v);
                v.Assign(r);
            }      

            return u;
        }

        // simple Euclidean least-common-multiple calculation.
        public static BigNum Lcm(BigNum u, BigNum v)
        {
            if (v.CompareTo(1) == 0)
                return new BigNum(u);

            if (u.CompareTo(1) == 0)
                return new BigNum(v);

            BigNum gcd = Gcd(u, v);
            BigNum result = LongDiv(u, gcd);
            result = LongMul(result, v);

            return result;
        }

        // set the current BigInt to zero.
        public void Zero()
        {
            if (size != 0)
            {
                EnsureIndex(0);
                Array.Clear(digits, 0, size);
                isNegative = false;
                size = 0;
            }
        }

        // read in a BigInt from a string.
        // REVIEW (RandyTh): should I try to handle improper input here?

        // REVIEW PERF (RandyTh): this and the following routine could be
        // made much simpler and more efficient if I were to decide to
        // insist on digitBase always being a power of 10.  However, that
        // would yield slightly less efficient numerical computations for
        // the rest of the code.  For now, I've just taken the first step
        // in moving the complicated case into a separate function, so we
        // could potentially switch between them at compile time.  Just
        // don't be too surprised in the meantime about string<->bignum
        // conversion being nonlinear.

        public static BigNum Parse(string strNum)
        {
            BigNum result = new BigNum();
            result.ParseNonBase10(strNum);
            return result;
        }
        
        public void ParseNonBase10(string strNum)
        {
            Zero();
            if (strNum.Length > 0)
            {
                int i;
                int digitVal = 0;
                int iMin = 0;

                if (strNum[0] == '-')
                {
                    iMin = 1;
                }

                // pretty straightforward here, just pull character
                // digits off the input and accumulate a summed product
                // of the digits in our BigNum.
                for (i = iMin; i < strNum.Length; ++i)
                {
                    ShortMul(10);
                    Debug.Assert(strNum[i] >= '0' && strNum[i] <= '9');
                    digitVal = strNum[i] - '0';
                    Add(digitVal);
                }

                if (iMin == 1 && strNum.Length > 1 && strNum[1] != '0')
                    isNegative = true;
            }
        }
  
        public override string ToString()
        {
            return ToStringNonBase10();
        }

        // REVIEW PERF (RandyTh): Again, note that this code has nonlinear
        // efficiency.  See above on how this could be improved.  I do
        // think this is pretty reasonable for small numbers, though.
        public string ToStringNonBase10()
        {
            if (size == 0)
            {
                return "0";
            }
            else
            {
                BigNum bnTemp = new BigNum(this);
                StringBuilder sb = new StringBuilder();

                // We accumulate digits in our buffer from
                // least-significant to most-significant, which is simple
                // to code, but requires a reverse at the end.
                int iRem = 0;
                while (bnTemp.size > 0)
                {
                    bnTemp.ShortDiv(10, out iRem);
                    sb.Append((char) (iRem + '0'));
                }

                // append the sign at the end, which is about to become
                // the beginning.
                if (isNegative)
                    sb.Append('-');

                // reverse the string for output.
                int i;
                for (i = 0; i < sb.Length / 2; ++i)
                {
                    char temp = sb[i];
                    int iIndex = sb.Length - i - 1;
                    sb[i] = sb[iIndex];
                    sb[iIndex] = temp;
                }

                return sb.ToString();
            }
        }

        // simple primitive parity check.
        public bool Even()
        {
            // REVIEW PERF (RandyTh): doing the slower zero extend check
            // shouldn't be necessary, but I keep debating with myself
            // about whether I should always ensure the 0th place is
            // present (i.e. that the array is always allocated).
            int i = GetDigit(0);
            return (i % 2) == 0 ? true : false;
        }

        // ensure that the given index is properly initialized
        // for this number.
        private void EnsureIndex(int index)
        {
            // REVIEW (RandyTh): I'm suppressing the null check
            // here.  This will have to change if I ever delay
            // initialize the array.
            
            if (index >= digits.Length)
            {
                int newLength = Math.Max(digits.Length * 2, index + 1);
                int[] newDigits = new int[newLength];

                if (digits != null)
                {
                    Array.Clear(newDigits, 0, newDigits.Length);
                    Array.Copy(digits, newDigits, digits.Length);
                }
                digits = newDigits;
            }

            // REVIEW PERF (RandyTh): this zero extension could be
            // optimized out if I were willing to keep track about how
            // far we are zeroed out to.  If I always ensure that the
            // array is zero-extended on allocation, I could forgo this.
            if (index >= size && index != 0)
            {
                Array.Clear(digits, size, index - size + 1);
            }
        }

        // get the given digit of the number, zero extended to infinity.
        private int GetDigit(int index)
        {
            if (index < size)
            {
                return digits[index];
            }
            else
            {
                return 0;
            }
        }

        // set the given digit, growing the number as needed beforehand.
        private void SetDigit(int index, int digitValue)
        {
            EnsureIndex(index);
            digits[index] = digitValue;
        }

        // clean up any overflowing digits and recalculate the size
        // of this number.
        //
        // REVIEW (RandyTh): this is sort of cheesy -- it would be
        // better to do this as part of whatever primary operation
        // is being performed, as it would be more efficient.  This
        // is always O(N) on the length of the vector, but frequently
        // only a constant amount of work is needed.
        
        private void Normalize()
        {
            int i;
            int carryTemp = 0;
            int maxSize = 0;
            for (i = 0; i < digits.Length; ++i)
            {
                digits[i] += carryTemp;

                carryTemp = 0;

                if ((digits[i] >= digitBase) || (digits[i] <= -digitBase))
                {
                    carryTemp += Math.DivRem(
                        digits[i], digitBase, out digits[i]);
                    while (digits[i] < 0)
                    {
                        digits[i] += digitBase;
                        --carryTemp;
                    }
                }
                else
                {
                    carryTemp = 0;
                }

                if (digits[i] != 0)
                    maxSize = i + 1;
            }

            if (carryTemp < 0)
            {
                isNegative = true;
            }
            else if (carryTemp > 0)
            {
                SetDigit(digits.Length, carryTemp);
                size = maxSize + 1;
            }
            else
            {
                size = maxSize;
            }
        }
    }

    // A Rational is stored as a numerator and a denominator.  On
    // construction, they're reduced to lowest terms by the Euclidean
    // algorithm.  All this is probably terribly inefficient.

    public class Rational : Number, IComparable<Rational>
    {
        private BigNum num = new BigNum(0);
        private BigNum denom = new BigNum(1); 

        public BigNum Num { get { return num; } }
        public BigNum Denom { get { return denom; } }

        // REVIEW (RandyTh): this is a good algorithm for cases where
        // u and v are of greatly different magnitudes, but is a little
        // slower than direct subtraction when the numbers are close in
        // magnitude.  At least I believe that this degrades well.
        private static int FixNumGcd(int i, int j)
        {
            if (j == 1)
                return 1;

            if (i < 0)
                i *= -1;

            int r = 0;
            while (j != 0)
            {
                r = i % j;
                i = j;
                j = r;
            }

            return i;
        }

        public Rational Reciprocal
        {
            get { return new Rational(denom, num); }
        }

        // reduce the fraction after a computation.
        private void Reduce()
        {
            if (num.CompareTo(0) == 0)
            {
                denom.Assign(1);
                return;
            }

            if (denom.CompareTo(1) == 0)
            {
                return;
            }

            // By convention, we maintain the invariant that the numerator
            // is the only negative portion of the rational.
            if (Denom.IsNegative)
            {
                num.Negate();
                denom.Negate();
            }

            BigNum gcd = BigNum.Gcd(num, denom);
            num.Assign(BigNum.LongDiv(num, gcd));
            denom.Assign(BigNum.LongDiv(denom, gcd));
        }

        public Rational(Rational r)
        {
            num.Assign(r.Num);
            denom.Assign(r.Denom);

            // REVIEW (RandyTh): should we bother reducing here?
            // Reduce();
        }

        public Rational(BigNum n)
        {
            num.Assign(n);
            denom.Assign(1);
        }

        // REVIEW (RandyTh): This constructor can and has caused bugs.
        // Should I remove it?
        public Rational(int n)
        {
            num.Assign(n);
            denom.Assign(1);
        }
        
        public Rational(int n, int d)
        {
            if (d == 0)
                throw new MathException("Division by zero!");

            num.Assign(n);
            denom.Assign(d);
            this.Reduce();
        }

        public Rational(BigNum n, BigNum d)
        {
            if (d.CompareTo(0) == 0)
                throw new MathException("Division by zero!");

            num.Assign(n);
            denom.Assign(d);
            this.Reduce();
        }

        public Rational(int n, BigNum d)
        {
            if (d.CompareTo(0) == 0)
                throw new MathException("Division by zero!");

            num.Assign(n);
            denom.Assign(d);
            this.Reduce();
        }

        public Rational(BigNum n, int d)
        {
            if (d == 0)
                throw new MathException("Division by zero!");

            num.Assign(n);
            denom.Assign(d);
            this.Reduce();
        }

        // comparison routine
        public int CompareTo(Rational other)
        {
            // optimize for the case wher the two fractions have the same
            // denominator (e.g. 1, the integer case).
            if (Denom == other.Denom)
            {
                return Num.CompareTo(other.Num);
            }
            else
            {
                BigNum left = BigNum.LongMul(Num, other.Denom);
                BigNum right = BigNum.LongMul(other.Num, Denom);

                return left.CompareTo(right);
            }
        }

        public override string ToString()
        {
            StringBuilder fmt = new StringBuilder(Num.ToString());

            if (Denom.CompareTo(1) != 0)
            {
                fmt.Append("/");
                fmt.Append(Denom.ToString());
            }
            return fmt.ToString();
        }

        public void Negate()
        {
            num.Negate();
        }

        public static Rational Add(Rational a, Rational b)
        {
            Rational result = new Rational(a);
            result.Add(b);
            return result;
        }

        // mutating form of add.
        public void Add(Rational a)
        {
            // optimize for the common case of equal denominators.
            if (Denom.CompareTo(a.Denom) == 0)
            {
                num.Add(a.Num);
            }
            else
            {
                // do the mixed denominator version.
                //
                // REVIEW PERF (RandyTh): is it beneficial to do GCD
                // reduction here?  I think the main tradeoff here is
                // increased GC usage vs. preventing the intermediate
                // results from growing to be as large.
                BigNum gcd = BigNum.Gcd(denom, a.Denom);
                num.Assign(
                    BigNum.Add(
                        BigNum.LongMul(
                            BigNum.LongDiv(Num, gcd), a.Denom),
                        BigNum.LongMul(
                            BigNum.LongDiv(a.Num, gcd), Denom)));
                denom.Assign(
                    BigNum.LongMul(
                        BigNum.LongDiv(
                            denom, gcd),
                        a.Denom));
            }
            this.Reduce();
        }

        // just like addition -- not much to see here.
        public static Rational Subtract(Rational a, Rational b)
        {
            Rational result = new Rational(a);
            result.Subtract(b);
            return result;
        }

        public void Subtract(Rational a)
        {
            if (Denom.CompareTo(a.Denom) == 0)
            {
                num.Assign(BigNum.Sub(Num, a.Num));
            }
            else
            {
                BigNum gcd = BigNum.Gcd(denom, a.Denom);
                num.Assign(
                    BigNum.Sub(
                        BigNum.LongMul(
                            BigNum.LongDiv(a.Denom, gcd), Num),
                        BigNum.LongMul(
                            BigNum.LongDiv(Denom, gcd), a.Num)));
                denom.Assign(
                    BigNum.LongMul(
                        BigNum.LongDiv(
                            denom, gcd),
                        a.Denom));
                this.Reduce();
            }
            this.Reduce();
        }

        public static Rational Multiply(Rational a, Rational b)
        {
            Rational result = new Rational(a);
            result.Multiply(b);
            return result;
        }

        public void Multiply(Rational a)
        {
            num.Assign(BigNum.LongMul(Num, a.Num));

            if (a.Denom.CompareTo(1) != 0 || Denom.CompareTo(a.Denom) != 0)
            {
                denom.Assign(BigNum.LongMul(Denom, a.Denom));
            }
            
            this.Reduce();
        }

        public BigNum Quotient()
        {
            return BigNum.LongDiv(Num, Denom);
        }

        public BigNum Remainder()
        {
            BigNum rem = new BigNum();
            BigNum.LongDiv(Num, Denom, out rem);
            return rem;
        }

        public BigNum Round()
        {
            if (Denom.CompareTo(1) == 0)
            {
                return new BigNum(Num);
            }
            else
            {
                Rational r = Add(this, new Rational(1, 2));

                // Per the Scheme standard, half values must round
                // to the nearest even integer. This means that half the
                // time, we overshoot when we round these values up.
                if (r.Denom.CompareTo(1) == 0)
                {
                    if (r.Num.Even())
                    {
                        return r.Num;
                    }
                    else
                    {
                        r.Num.Add(-1);
                        return r.Num;
                    }
                }
                else
                {
                    return r.Quotient();
                }
            }
        }

        public BigNum Floor()
        {
            BigNum rem;
            BigNum result = BigNum.LongDiv(Num, Denom, out rem);
            if (rem.CompareTo(0) != 0)
            {
                result.Add(-1);
            }
            
            return result;
        }
    }
}
