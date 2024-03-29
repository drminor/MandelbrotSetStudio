/*
fixed_point_math tutorial
- A tutorial-like practice code to learn how to do fixed-point math, manual "float"-like prints using integers only,
  "float"-like integer rounding, and fractional fixed-point math on large integers.

By Gabriel Staples
www.ElectricRCAircraftGuy.com
- email available via the Contact Me link at the top of my website.
Started: 22 Dec. 2018
Updated: 25 Dec. 2018

References:
- https://stackoverflow.com/questions/10067510/fixed-point-arithmetic-in-c-programming

Commands to Compile & Run:
As a C program (the file must NOT have a C++ file extension or it will be automatically compiled as C++, so we will
make a copy of it and change the file extension to .c first):
See here: https://stackoverflow.com/a/3206195/4561887.
    cp fixed_point_math.cpp fixed_point_math_copy.c && gcc -Wall -std=c99 -o ./bin/fixed_point_math_c fixed_point_math_copy.c && ./bin/fixed_point_math_c
As a C++ program:
    g++ -Wall -o ./bin/fixed_point_math_cpp fixed_point_math.cpp && ./bin/fixed_point_math_cpp

*/

#include <stdbool.h>
#include <stdio.h>
#include <stdint.h>

// Define our fixed point type.
typedef uint32_t fixed_point_t;

#define BITS_PER_BYTE 8

#define FRACTION_BITS 16 // 1 << 16 = 2^16 = 65536
#define FRACTION_DIVISOR (1 << FRACTION_BITS)
#define FRACTION_MASK (FRACTION_DIVISOR - 1) // 65535 (all LSB set, all MSB clear)

// // Conversions [NEVERMIND, LET'S DO THIS MANUALLY INSTEAD OF USING THESE MACROS TO HELP ENGRAIN IT IN US BETTER]:
// #define INT_2_FIXED_PT_NUM(num)     (num << FRACTION_BITS)      // Regular integer number to fixed point number
// #define FIXED_PT_NUM_2_INT(fp_num)  (fp_num >> FRACTION_BITS)   // Fixed point number back to regular integer number

// Private function prototypes:
static void print_if_error_introduced(uint8_t num_digits_after_decimal);

int main(int argc, char* argv[])
{
    printf("Begin.\n");

    // We know how many bits we will use for the fraction, but how many bits are remaining for the whole number, 
    // and what's the whole number's max range? Let's calculate it.
    const uint8_t WHOLE_NUM_BITS = sizeof(fixed_point_t) * BITS_PER_BYTE - FRACTION_BITS;
    const fixed_point_t MAX_WHOLE_NUM = (1 << WHOLE_NUM_BITS) - 1;
    printf("fraction bits = %u.\n", FRACTION_BITS);
    printf("whole number bits = %u.\n", WHOLE_NUM_BITS);
    printf("max whole number = %u.\n\n", MAX_WHOLE_NUM);

    // Create a variable called `price`, and let's do some fixed point math on it.
    const fixed_point_t PRICE_ORIGINAL = 503;
    fixed_point_t price = PRICE_ORIGINAL << FRACTION_BITS;
    price += 10 << FRACTION_BITS;
    price *= 3;
    price /= 7; // now our price is ((500 + 10)*3/7) = 218.571428571.

    printf("price as a true double is %3.9f.\n", ((double)PRICE_ORIGINAL + 10) * 3 / 7);
    printf("price as integer is %u.\n", price >> FRACTION_BITS);
    printf("price fractional part is %u (of %u).\n", price & FRACTION_MASK, FRACTION_DIVISOR);
    printf("price fractional part as decimal is %f (%u/%u).\n", (double)(price & FRACTION_MASK) / FRACTION_DIVISOR,
        price & FRACTION_MASK, FRACTION_DIVISOR);

    // Now, if you don't have float support (neither in hardware via a Floating Point Unit [FPU], nor in software
    // via built-in floating point math libraries as part of your processor's C implementation), then you may have
    // to manually print the whole number and fractional number parts separately as follows. Look for the patterns.
    // Be sure to make note of the following 2 points:
    // - 1) the digits after the decimal are determined by the multiplier: 
    //     0 digits: * 10^0 ==> * 1         <== 0 zeros
    //     1 digit : * 10^1 ==> * 10        <== 1 zero
    //     2 digits: * 10^2 ==> * 100       <== 2 zeros
    //     3 digits: * 10^3 ==> * 1000      <== 3 zeros
    //     4 digits: * 10^4 ==> * 10000     <== 4 zeros
    //     5 digits: * 10^5 ==> * 100000    <== 5 zeros
    // - 2) Be sure to use the proper printf format statement to enforce the proper number of leading zeros in front of
    //   the fractional part of the number. ie: refer to the "%01", "%02", "%03", etc. below.
    // Manual "floats":
    // 0 digits after the decimal
    printf("price (manual float, 0 digits after decimal) is %u.",
        price >> FRACTION_BITS); print_if_error_introduced(0);
    // 1 digit after the decimal
    printf("price (manual float, 1 digit  after decimal) is %u.%01llu.",
        price >> FRACTION_BITS, (uint64_t)(price & FRACTION_MASK) * 10 / FRACTION_DIVISOR);
    print_if_error_introduced(1);
    // 2 digits after decimal
    printf("price (manual float, 2 digits after decimal) is %u.%02llu.",
        price >> FRACTION_BITS, (uint64_t)(price & FRACTION_MASK) * 100 / FRACTION_DIVISOR);
    print_if_error_introduced(2);
    // 3 digits after decimal
    printf("price (manual float, 3 digits after decimal) is %u.%03llu.",
        price >> FRACTION_BITS, (uint64_t)(price & FRACTION_MASK) * 1000 / FRACTION_DIVISOR);
    print_if_error_introduced(3);
    // 4 digits after decimal
    printf("price (manual float, 4 digits after decimal) is %u.%04llu.",
        price >> FRACTION_BITS, (uint64_t)(price & FRACTION_MASK) * 10000 / FRACTION_DIVISOR);
    print_if_error_introduced(4);
    // 5 digits after decimal
    printf("price (manual float, 5 digits after decimal) is %u.%05llu.",
        price >> FRACTION_BITS, (uint64_t)(price & FRACTION_MASK) * 100000 / FRACTION_DIVISOR);
    print_if_error_introduced(5);
    // 6 digits after decimal
    printf("price (manual float, 6 digits after decimal) is %u.%06llu.",
        price >> FRACTION_BITS, (uint64_t)(price & FRACTION_MASK) * 1000000 / FRACTION_DIVISOR);
    print_if_error_introduced(6);
    printf("\n");


    // Manual "floats" ***with rounding now***:
    // - To do rounding with integers, the concept is best understood by examples: 
    // BASE 10 CONCEPT:
    // 1. To round to the nearest whole number: 
    //    Add 1/2 to the number, then let it be truncated since it is an integer. 
    //    Examples:
    //      1.5 + 1/2 = 1.5 + 0.5 = 2.0. Truncate it to 2. Good!
    //      1.99 + 0.5 = 2.49. Truncate it to 2. Good!
    //      1.49 + 0.5 = 1.99. Truncate it to 1. Good!
    // 2. To round to the nearest tenth place:
    //    Multiply by 10 (this is equivalent to doing a single base-10 left-shift), then add 1/2, then let 
    //    it be truncated since it is an integer, then divide by 10 (this is a base-10 right-shift).
    //    Example:
    //      1.57 x 10 + 1/2 = 15.7 + 0.5 = 16.2. Truncate to 16. Divide by 10 --> 1.6. Good.
    // 3. To round to the nearest hundredth place:
    //    Multiply by 100 (base-10 left-shift 2 places), add 1/2, truncate, divide by 100 (base-10 
    //    right-shift 2 places).
    //    Example:
    //      1.579 x 100 + 1/2 = 157.9 + 0.5 = 158.4. Truncate to 158. Divide by 100 --> 1.58. Good.
    //
    // BASE 2 CONCEPT:
    // - We are dealing with fractional numbers stored in base-2 binary bits, however, and we have already 
    //   left-shifted by FRACTION_BITS (num << FRACTION_BITS) when we converted our numbers to fixed-point 
    //   numbers. Therefore, *all we have to do* is add the proper value, and we get the same effect when we 
    //   right-shift by FRACTION_BITS (num >> FRACTION_BITS) in our conversion back from fixed-point to regular
    //   numbers. Here's what that looks like for us:
    // - Note: "addend" = "a number that is added to another".
    //   (see https://www.google.com/search?q=addend&oq=addend&aqs=chrome.0.0l6.1290j0j7&sourceid=chrome&ie=UTF-8).
    // - Rounding to 0 digits means simply rounding to the nearest whole number.
    // Round to:        Addends:
    // 0 digits: add 5/10 * FRACTION_DIVISOR       ==> + FRACTION_DIVISOR/2
    // 1 digits: add 5/100 * FRACTION_DIVISOR      ==> + FRACTION_DIVISOR/20
    // 2 digits: add 5/1000 * FRACTION_DIVISOR     ==> + FRACTION_DIVISOR/200
    // 3 digits: add 5/10000 * FRACTION_DIVISOR    ==> + FRACTION_DIVISOR/2000
    // 4 digits: add 5/100000 * FRACTION_DIVISOR   ==> + FRACTION_DIVISOR/20000
    // 5 digits: add 5/1000000 * FRACTION_DIVISOR  ==> + FRACTION_DIVISOR/200000
    // 6 digits: add 5/10000000 * FRACTION_DIVISOR ==> + FRACTION_DIVISOR/2000000
    // etc.

    printf("WITH MANUAL INTEGER-BASED ROUNDING:\n");

    // Calculate addends used for rounding (see definition of "addend" above).
    fixed_point_t addend0 = FRACTION_DIVISOR / 2;
    fixed_point_t addend1 = FRACTION_DIVISOR / 20;
    fixed_point_t addend2 = FRACTION_DIVISOR / 200;
    fixed_point_t addend3 = FRACTION_DIVISOR / 2000;
    fixed_point_t addend4 = FRACTION_DIVISOR / 20000;
    fixed_point_t addend5 = FRACTION_DIVISOR / 200000;

    // Print addends used for rounding.
    printf("addend0 = %u.\n", addend0);
    printf("addend1 = %u.\n", addend1);
    printf("addend2 = %u.\n", addend2);
    printf("addend3 = %u.\n", addend3);
    printf("addend4 = %u.\n", addend4);
    printf("addend5 = %u.\n", addend5);

    // Calculate rounded prices
    fixed_point_t price_rounded0 = price + addend0; // round to 0 decimal digits
    fixed_point_t price_rounded1 = price + addend1; // round to 1 decimal digits
    fixed_point_t price_rounded2 = price + addend2; // round to 2 decimal digits
    fixed_point_t price_rounded3 = price + addend3; // round to 3 decimal digits
    fixed_point_t price_rounded4 = price + addend4; // round to 4 decimal digits
    fixed_point_t price_rounded5 = price + addend5; // round to 5 decimal digits

    // Print manually rounded prices of manually-printed fixed point integers as though they were "floats".
    printf("rounded price (manual float, rounded to 0 digits after decimal) is %u.\n",
        price_rounded0 >> FRACTION_BITS);
    printf("rounded price (manual float, rounded to 1 digit  after decimal) is %u.%01llu.\n",
        price_rounded1 >> FRACTION_BITS, (uint64_t)(price_rounded1 & FRACTION_MASK) * 10 / FRACTION_DIVISOR);
    printf("rounded price (manual float, rounded to 2 digits after decimal) is %u.%02llu.\n",
        price_rounded2 >> FRACTION_BITS, (uint64_t)(price_rounded2 & FRACTION_MASK) * 100 / FRACTION_DIVISOR);
    printf("rounded price (manual float, rounded to 3 digits after decimal) is %u.%03llu.\n",
        price_rounded3 >> FRACTION_BITS, (uint64_t)(price_rounded3 & FRACTION_MASK) * 1000 / FRACTION_DIVISOR);
    printf("rounded price (manual float, rounded to 4 digits after decimal) is %u.%04llu.\n",
        price_rounded4 >> FRACTION_BITS, (uint64_t)(price_rounded4 & FRACTION_MASK) * 10000 / FRACTION_DIVISOR);
    printf("rounded price (manual float, rounded to 5 digits after decimal) is %u.%05llu.\n",
        price_rounded5 >> FRACTION_BITS, (uint64_t)(price_rounded5 & FRACTION_MASK) * 100000 / FRACTION_DIVISOR);


    // =================================================================================================================

    printf("\nRELATED CONCEPT: DOING LARGE-INTEGER MATH WITH SMALL INTEGER TYPES:\n");

    // RELATED CONCEPTS:
    // Now let's practice handling (doing math on) large integers (ie: large relative to their integer type),
    // withOUT resorting to using larger integer types (because they may not exist for our target processor), 
    // and withOUT using floating point math, since that might also either not exist for our processor, or be too
    // slow or program-space-intensive for our application.
    // - These concepts are especially useful when you hit the limits of your architecture's integer types: ex: 
    //   if you have a uint64_t nanosecond timestamp that is really large, and you need to multiply it by a fraction
    //   to convert it, but you don't have uint128_t types available to you to multiply by the numerator before 
    //   dividing by the denominator. What do you do?
    // - We can use fixed-point math to achieve desired results. Let's look at various approaches.
    // - Let's say my goal is to multiply a number by a fraction < 1 withOUT it ever growing into a larger type.
    // - Essentially we want to multiply some really large number (near its range limit for its integer type)
    //   by some_number/some_larger_number (ie: a fraction < 1). The problem is that if we multiply by the numerator
    //   first, it will overflow, and if we divide by the denominator first we will lose resolution via bits 
    //   right-shifting out.
    // Here are various examples and approaches.

    // -----------------------------------------------------
    // EXAMPLE 1
    // Goal: Use only 16-bit values & math to find 65401 * 16/127.
    // Result: Great! All 3 approaches work, with the 3rd being the best. To learn the techniques required for the 
    // absolute best approach of all, take a look at the 8th approach in Example 2 below.
    // -----------------------------------------------------
    uint16_t num16 = 65401; // 1111 1111 0111 1001 
    uint16_t times = 16;
    uint16_t divide = 127;

    printf("\nEXAMPLE 1\n");

    // Find the true answer.
    // First, let's cheat to know the right answer by letting it grow into a larger type. 
    // Multiply *first* (before doing the divide) to avoid losing resolution.
    //printf("%u * %u/%u = %u. <== true answer\n", num16, times, divide, (uint32_t)num16 * times / divide);
    printf("%u * %u/%u = %llu. <== true answer\n", num16, times, divide, ((uint64_t)num16) * times);

    // 1st approach: just divide first to prevent overflow, and lose precision right from the start.
    uint32_t num16_result = num16 * times;
    printf("1st approach (divide then multiply):\n");
    printf("  num16_result = %u. <== Loses bits that right-shift out during the initial divide.\n", num16_result);

    // 2nd approach: split the 16-bit number into 2 8-bit numbers stored in 16-bit numbers, 
    // placing all 8 bits of each sub-number to the ***far right***, with 8 bits on the left to grow
    // into when multiplying. Then, multiply and divide each part separately. 
    // - The problem, however, is that you'll lose meaningful resolution on the upper-8-bit number when you 
    //   do the division, since there's no bits to the right for the right-shifted bits during division to 
    //   be retained in.
    // Re-sum both sub-numbers at the end to get the final result. 
    // - NOTE THAT 257 IS THE HIGHEST *TIMES* VALUE I CAN USE SINCE 2^16/0b0000,0000,1111,1111 = 65536/255 = 257.00392.
    //   Therefore, any *times* value larger than this will cause overflow.
    uint16_t num16_upper8 = num16 >> 8; // 1111 1111
    uint16_t num16_lower8 = num16 & 0xFF; // 0111 1001
    num16_upper8 *= times;
    num16_lower8 *= times;
    //num16_upper8 /= divide;
    //num16_lower8 /= divide;
    num16_result = (num16_upper8 << 16) + num16_lower8;
    printf("2nd approach (split into 2 8-bit sub-numbers with bits at far right):\n");
    printf("  num16_result = %u. <== Loses bits that right-shift out during the divide.\n", num16_result);

    // 3rd approach: split the 16-bit number into 2 8-bit numbers stored in 16-bit numbers, 
    // placing all 8 bits of each sub-number ***in the center***, with 4 bits on the left to grow when 
    // multiplying and 4 bits on the right to not lose as many bits when dividing. 
    // This will help stop the loss of resolution when we divide, at the cost of overflowing more easily when we 
    // multiply.
    // - NOTE THAT 16 IS THE HIGHEST *TIMES* VALUE I CAN USE SINCE 2^16/0b0000,1111,1111,0000 = 65536/4080 = 16.0627.
    //   Therefore, any *times* value larger than this will cause overflow.
    num16_upper8 = (num16 >> 4) & 0x0FF0;
    num16_lower8 = (num16 << 4) & 0x0FF0;
    num16_upper8 *= times;
    num16_lower8 *= times;
    //num16_upper8 /= divide;
    //num16_lower8 /= divide;
    num16_result = (num16_upper8 << 4) + (num16_lower8 >> 4);
    printf("3rd approach (split into 2 8-bit sub-numbers with bits centered):\n");
    printf("  num16_result = %u. <== Perfect! Retains the bits that right-shift during the divide.\n", num16_result);

    // -----------------------------------------------------
    // EXAMPLE 2
    // Goal: Use only 16-bit values & math to find 65401 * 99/127.
    // Result: Many approaches work, so long as enough bits exist to the left to not allow overflow during the 
    // multiply. The best approach is the 8th one, however, which 1) right-shifts the minimum possible before the
    // multiply, in order to retain as much resolution as possible, and 2) does integer rounding during the divide
    // in order to be as accurate as possible. This is the best approach to use.
    // -----------------------------------------------------
    num16 = 65401; // 1111 1111 0111 1001 
    times = 99;
    divide = 127;

    printf("\nEXAMPLE 2\n");

    // Find the true answer by letting it grow into a larger type.
    printf("%u * %u/%u = %llu. <== true answer\n", num16, times, divide, ((uint64_t)num16) * times);

    // 1st approach: just divide first to prevent overflow, and lose precision right from the start.
    num16_result = num16 * times;
    printf("1st approach (divide then multiply):\n");
    printf("  num16_result = %u. <== Loses bits that right-shift out during the initial divide.\n", num16_result);

    // 2nd approach: split the 16-bit number into 2 8-bit numbers stored in 16-bit numbers, 
    // placing all 8 bits of each sub-number to the ***far right***, with 8 bits on the left to grow
    // into when multiplying. Then, multiply and divide each part separately. 
    // - The problem, however, is that you'll lose meaningful resolution on the upper-8-bit number when you 
    //   do the division, since there's no bits to the right for the right-shifted bits during division to 
    //   be retained in.
    // Re-sum both sub-numbers at the end to get the final result. 
    // - NOTE THAT 257 IS THE HIGHEST *TIMES* VALUE I CAN USE SINCE 2^16/0b0000,0000,1111,1111 = 65536/255 = 257.00392.
    //   Therefore, any *times* value larger than this will cause overflow.
    num16_upper8 = num16 >> 8; // 1111 1111
    num16_lower8 = num16 & 0xFF; // 0111 1001
    num16_upper8 *= times;
    num16_lower8 *= times;
    //num16_upper8 /= divide;
    //num16_lower8 /= divide;
    num16_result = (num16_upper8 << 8) + num16_lower8;
    printf("2nd approach (split into 2 8-bit sub-numbers with bits at far right):\n");
    printf("  num16_result = %ul. <== Loses bits that right-shift out during the divide.\n", num16_result);

    // 3rd approach: split the 16-bit number into 2 8-bit numbers stored in 16-bit numbers, 
    // placing all 8 bits of each sub-number ***in the center***, with 4 bits on the left to grow when 
    // multiplying and 4 bits on the right to not lose as many bits when dividing. 
    // This will help stop the loss of resolution when we divide, at the cost of overflowing more easily when we 
    // multiply.
    // - NOTE THAT 16 IS THE HIGHEST *TIMES* VALUE I CAN USE SINCE 2^16/0b0000,1111,1111,0000 = 65536/4080 = 16.0627.
    //   Therefore, any *times* value larger than this will cause overflow.
    num16_upper8 = (num16 >> 4) & 0x0FF0;
    num16_lower8 = (num16 << 4) & 0x0FF0;
    num16_upper8 *= times;
    num16_lower8 *= times;
    //num16_upper8 /= divide;
    //num16_lower8 /= divide;
    num16_result = (num16_upper8 << 4) + (num16_lower8 >> 4);
    printf("\n3rd approach (split into 2 8-bit sub-numbers with bits centered):\n");
    printf("  num16_result = %u. <== Completely wrong due to overflow during the multiply.\n", num16_result);

    // For the next approaches:
    uint16_t num16_1;
    uint16_t num16_2;
    uint16_t num16_3;
    uint16_t num16_4;
    uint16_t num16_5;
    uint16_t num16_6;
    uint16_t num16_7;
    uint16_t num16_8;
    uint16_t num16_9;
    uint16_t num16_10;
    uint16_t num16_11;
    uint16_t num16_12;
    uint16_t num16_13;
    uint16_t num16_14;
    uint16_t num16_15;
    uint16_t num16_16;

    // 4th approach: split the 16-bit number into 4 4-bit numbers, placing each sub-number ***in the center***
    // with 6 bits on the left and 6 bits on the right.
    // - Highest *times* value I can use is 2^16/0b0000,0011,1100,0000 = 65536/960 = 68.2667
    // - MSbits will be in num16_1, LSbits will be in num16_4.
    num16_1 = (num16 >> 6) & 0b0000001111000000;
    num16_2 = (num16 >> 2) & 0b0000001111000000;
    num16_3 = (num16 << 2) & 0b0000001111000000;
    num16_4 = (num16 << 6) & 0b0000001111000000;
    num16_1 *= times;
    num16_2 *= times;
    num16_3 *= times;
    num16_4 *= times;
    //num16_1 /= divide;
    //num16_2 /= divide;
    //num16_3 /= divide;
    //num16_4 /= divide;
    num16_result = (num16_1 << 6) + (num16_2 << 2) + (num16_3 >> 2) + (num16_4 >> 6);
    printf("4th approach (split into 4 4-bit sub-numbers with bits centered):\n");
    printf("  num16_result = %u. <== Completely wrong due to overflow during the multiply.\n", num16_result);

    // 5th approach: split into 8 2-bit numbers, ***centering*** each, with 7 bits on the left and 7 bits on the right. 
    // - Highest *times* value I can use is 2^16/0b0000,0001,1000,0000 = 65536/384 = 170.6667.
    // - MSbits will be in num16_1, LSbits will be in num16_8.
    num16_1 = (num16 >> 7) & 0b0000000110000000;
    num16_2 = (num16 >> 5) & 0b0000000110000000;
    num16_3 = (num16 >> 3) & 0b0000000110000000;
    num16_4 = (num16 >> 1) & 0b0000000110000000;
    num16_5 = (num16 << 1) & 0b0000000110000000;
    num16_6 = (num16 << 3) & 0b0000000110000000;
    num16_7 = (num16 << 5) & 0b0000000110000000;
    num16_8 = (num16 << 7) & 0b0000000110000000;
    num16_1 *= times;
    num16_2 *= times;
    num16_3 *= times;
    num16_4 *= times;
    num16_5 *= times;
    num16_6 *= times;
    num16_7 *= times;
    num16_8 *= times;
    //num16_1 /= divide;
    //num16_2 /= divide;
    //num16_3 /= divide;
    //num16_4 /= divide;
    //num16_5 /= divide;
    //num16_6 /= divide;
    //num16_7 /= divide;
    //num16_8 /= divide;
    num16_result = (num16_1 << 7) + (num16_2 << 5) + (num16_3 << 3) + (num16_4 << 1) +
        (num16_5 >> 1) + (num16_6 >> 3) + (num16_7 >> 5) + (num16_8 >> 7);
    printf("5th approach (split into 8 2-bit sub-numbers with bits centered):\n");
    printf("  num16_result = %u. <== Loses a few bits that right-shift out during the divide.\n", num16_result);

    // 6th approach: split into 16 1-bit numbers, each ***skewed left of center***, with 6 bits on the left and
    // 9 bits on the right, in order to get just enough extra *range* (bits on the left) as required to not 
    // overflow during the multiply, while maintaining as much *resolution* (bits on the right) a possible in order
    // to lose as few bits as possible during the divide.
    // - Highest *times* value I can use is 2^16/0b0000,0010,0000,0000 = 65536/512 = 128.
    // - MSbits will be in num16_1, LSbits will be in num16_16.
    num16_1 = (num16 >> 6) & 0b0000001000000000;
    num16_2 = (num16 >> 5) & 0b0000001000000000;
    num16_3 = (num16 >> 4) & 0b0000001000000000;
    num16_4 = (num16 >> 3) & 0b0000001000000000;
    num16_5 = (num16 >> 2) & 0b0000001000000000;
    num16_6 = (num16 >> 1) & 0b0000001000000000;
    num16_7 = (num16 >> 0) & 0b0000001000000000;
    num16_8 = (num16 << 1) & 0b0000001000000000;
    num16_9 = (num16 << 2) & 0b0000001000000000;
    num16_10 = (num16 << 3) & 0b0000001000000000;
    num16_11 = (num16 << 4) & 0b0000001000000000;
    num16_12 = (num16 << 5) & 0b0000001000000000;
    num16_13 = (num16 << 6) & 0b0000001000000000;
    num16_14 = (num16 << 7) & 0b0000001000000000;
    num16_15 = (num16 << 8) & 0b0000001000000000;
    num16_16 = (num16 << 9) & 0b0000001000000000;
    num16_1 *= times;
    num16_2 *= times;
    num16_3 *= times;
    num16_4 *= times;
    num16_5 *= times;
    num16_6 *= times;
    num16_7 *= times;
    num16_8 *= times;
    num16_9 *= times;
    num16_10 *= times;
    num16_11 *= times;
    num16_12 *= times;
    num16_13 *= times;
    num16_14 *= times;
    num16_15 *= times;
    num16_16 *= times;
    //num16_1 /= divide;
    //num16_2 /= divide;
    //num16_3 /= divide;
    //num16_4 /= divide;
    //num16_5 /= divide;
    //num16_6 /= divide;
    //num16_7 /= divide;
    //num16_8 /= divide;
    //num16_9 /= divide;
    //num16_10 /= divide;
    //num16_11 /= divide;
    //num16_12 /= divide;
    //num16_13 /= divide;
    //num16_14 /= divide;
    //num16_15 /= divide;
    //num16_16 /= divide;
    num16_result = (num16_1 << 6) + (num16_2 << 5) + (num16_3 << 4) + (num16_4 << 3) +
        (num16_5 << 2) + (num16_6 << 1) + (num16_7 << 0) + (num16_8 >> 1) +
        (num16_9 >> 2) + (num16_10 >> 3) + (num16_11 >> 4) + (num16_12 >> 5) +
        (num16_13 >> 6) + (num16_14 >> 7) + (num16_15 >> 8) + (num16_16 >> 9);
    printf("6th approach (split into 16 1-bit sub-numbers with bits skewed left):\n");
    printf("  num16_result = %u. <== Loses the fewest possible bits that right-shift out during the divide.\n",
        num16_result);

    // 7th approach: exact same calculations and limitations and process as the 6th approach above, except
    // done in a more optimized and maintainable way, thereby requiring fewer steps and less program space
    // to calculate it.
    uint16_t num16_array[16];
    // Right-shifting these bits gives us the additional *range* we need, at the sacrifice of resolution, 
    // so we still must do this since we need the range.
    num16_array[0] = (num16 >> 6) & 0b0000001000000000;
    num16_array[1] = (num16 >> 5) & 0b0000001000000000;
    num16_array[2] = (num16 >> 4) & 0b0000001000000000;
    num16_array[3] = (num16 >> 3) & 0b0000001000000000;
    num16_array[4] = (num16 >> 2) & 0b0000001000000000;
    num16_array[5] = (num16 >> 1) & 0b0000001000000000;
    // Left-shifting these bits gives us additional *fractional resolution*, at the sacrifice of *range*, 
    // but since we would just right-shift these in the end and lose the fractional resolution anyway, 
    // there's really no benefit nor point in left-shifting these like we did before, so don't. Just bit-mask
    // them in place instead.
    num16_array[6] = num16 & 0b0000001000000000;
    num16_array[7] = num16 & 0b0000000100000000;
    num16_array[8] = num16 & 0b0000000010000000;
    num16_array[9] = num16 & 0b0000000001000000;
    num16_array[10] = num16 & 0b0000000000100000;
    num16_array[11] = num16 & 0b0000000000010000;
    num16_array[12] = num16 & 0b0000000000001000;
    num16_array[13] = num16 & 0b0000000000000100;
    num16_array[14] = num16 & 0b0000000000000010;
    num16_array[15] = num16 & 0b0000000000000001;
    // Now do all of the math in a single for loop.
    for (uint8_t i = 0; i < 16; i++)
    {
        num16_array[i] *= times;
        //num16_array[i] /= divide;
    }
    // Now sum the result, taking care to only shift where required based on what we did above.
    num16_result = (num16_array[0] << 6) + (num16_array[1] << 5) + (num16_array[2] << 4) + (num16_array[3] << 3) +
        (num16_array[4] << 2) + (num16_array[5] << 1);
    for (uint8_t i = 6; i < 16; i++)
    {
        num16_result += num16_array[i];
    }
    printf("7th approach (split into 16 1-bit sub-numbers with bits skewed left):\n");
    printf("  num16_result = %u. <== [same as 6th approach] Loses the fewest possible bits that right-shift out "
        "during the divide.\n", num16_result);

    // 8th approach: very similar to the 7th approach, except doing rounding *during the division*.
    // Reference my "eRCaGuy_analogReadXXbit.cpp" file here for info. on how this works:
    // https://github.com/ElectricRCAircraftGuy/eRCaGuy_analogReadXXbit/blob/master/eRCaGuy_analogReadXXbit.cpp
    // From my notes there:
    /*Integer math rounding notes:
    To do rounding with integers, during division, use the following formula:
    (dividend + divisor/2)/divisor.

    For example, instead of doing a/b, doing (a + b/2)/b will give you the integer value of a/b, rounded to the nearest
    whole integer.  This only works perfectly for even values of b.  If b is odd, the rounding is imperfect, since b/2
    will not yield a whole number.

    Examples:

    a = 1723; b = 16
    a/b = 107.6875 --> truncated to 107
    (a + b/2)/b = 108.1875 --> truncated to 108, which is the same thing as a/b rounded to the nearest whole integer

    a = 1720; b = 16
    a/b = 107.5 --> truncated to 107
    (a + b/2)/b = 108 exactly, which is the same thing as a/b rounded to the nearest whole integer

    a = 1719; b = 16
    a/b = 107.4375 --> truncated to 107
    (a + b/2)/b = 107.9375 --> truncated to 107, which is the same thing as a/b rounded to the nearest whole integer

    Why does this work?
    If you do the algebra, you will see that doing (a + b/2)/b is the same thing as doing a/b + 1/2, which will always
    force a value, when truncated, to truncate to the value that it otherwise would have rounded to. So, this works
    perfectly!  The only problem is that 1/2 is not a valid integer (it truncates to 0), so you must instead do it in
    the order of (a + b/2)/b, in order to make it all work out!
    */

    // - Looking at the rounding formula above, the highest *times* value I can use is calculated as follows:
    // max_num*times + divide/2 < 2^16
    // times < (2^16 - divide/2)/max_num, where max_num = 0b0000001000000000 and divide = 127 for this case.
    // times < (2^16 - 127/2)/512 = (65536 - 63.5)/512 = 127.875977. This is a tiny bit lower than the 6th approach,
    // where the max *times* value allowed is 128.
    // NB: notice that the max *times* value here is also a function of the *divide* value we are using, as well as
    // the max_num possible based on the number and location of bits we are using.

    // Right-shifting these bits gives us the additional *range* we need, at the sacrifice of resolution, 
    // so we still must do this since we need the range.
    num16_array[0] = (num16 >> 6) & 0b0000001000000000;
    num16_array[1] = (num16 >> 5) & 0b0000001000000000;
    num16_array[2] = (num16 >> 4) & 0b0000001000000000;
    num16_array[3] = (num16 >> 3) & 0b0000001000000000;
    num16_array[4] = (num16 >> 2) & 0b0000001000000000;
    num16_array[5] = (num16 >> 1) & 0b0000001000000000;
    // Left-shifting these bits gives us additional *fractional resolution*, at the sacrifice of *range*, 
    // but since we would just right-shift these in the end and lose the fractional resolution anyway, 
    // there's really no benefit nor point in left-shifting these like we did before, so don't. Just bit-mask
    // them in place instead.
    num16_array[6] = num16 & 0b0000001000000000;
    num16_array[7] = num16 & 0b0000000100000000;
    num16_array[8] = num16 & 0b0000000010000000;
    num16_array[9] = num16 & 0b0000000001000000;
    num16_array[10] = num16 & 0b0000000000100000;
    num16_array[11] = num16 & 0b0000000000010000;
    num16_array[12] = num16 & 0b0000000000001000;
    num16_array[13] = num16 & 0b0000000000000100;
    num16_array[14] = num16 & 0b0000000000000010;
    num16_array[15] = num16 & 0b0000000000000001;
    // Now do all of the math in a single for loop.
    for (uint8_t i = 0; i < 16; i++)
    {
        num16_array[i] *= times;
        // Don't forget to do integer rounding during the divide!
        // Ie: instead of doing a/b, do (a + b/2)/b.
        //num16_array[i] = (num16_array[i] + divide / 2) / divide;
    }
    // Now sum the result, taking care to only shift where required based on what we did above.
    num16_result = (num16_array[0] << 6) + (num16_array[1] << 5) + (num16_array[2] << 4) + (num16_array[3] << 3) +
        (num16_array[4] << 2) + (num16_array[5] << 1);
    for (uint8_t i = 6; i < 16; i++)
    {
        num16_result += num16_array[i];
    }
    printf("[BEST APPROACH OF ALL] 8th approach (split into 16 1-bit sub-numbers with bits skewed left, "
        "w/integer rounding during division):\n");
    printf("  num16_result = %u. <== Loses the fewest possible bits that right-shift out during the divide, \n"
        "  & has better accuracy due to rounding during the divide.\n", num16_result);

    return 0;
} // main

// PRIVATE FUNCTION DEFINITIONS:

/// @brief A function to help identify at what decimal digit error is introduced, based on how many bits you are using
///        to represent the fractional portion of the number in your fixed-point number system.
/// @details    Note: this function relies on an internal static bool to keep track of if it has already
///             identified at what decimal digit error is introduced, so once it prints this fact once, it will never 
///             print again. This is by design just to simplify usage in this demo.
/// @param[in]  num_digits_after_decimal    The number of decimal digits we are printing after the decimal 
///             (0, 1, 2, 3, etc)
/// @return     None
static void print_if_error_introduced(uint8_t num_digits_after_decimal)
{
    static bool already_found = false;

    // Array of power base 10 values, where the value = 10^index:
    const uint32_t POW_BASE_10[] =
    {
        1, // index 0 (10^0)
        10,
        100,
        1000,
        10000,
        100000,
        1000000,
        10000000,
        100000000,
        1000000000, // index 9 (10^9); 1 Billion: the max power of 10 that can be stored in a uint32_t
    };

    if (already_found == true)
    {
        goto done;
    }

    if (POW_BASE_10[num_digits_after_decimal] > FRACTION_DIVISOR)
    {
        already_found = true;
        printf(" <== Fixed-point math decimal error first\n"
            "    starts to get introduced here since the fixed point resolution (1/%u) now has lower resolution\n"
            "    than the base-10 resolution (which is 1/%u) at this decimal place. Decimal error may not show\n"
            "    up at this decimal location, per say, but definitely will for all decimal places hereafter.",
            FRACTION_DIVISOR, POW_BASE_10[num_digits_after_decimal]);
    }

done:
    printf("\n");
}