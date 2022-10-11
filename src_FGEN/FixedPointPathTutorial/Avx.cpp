#include <iostream>

//#ifdef __AVX__
//#include <immintrin.h>
//#else
//#warning No AVX support - will not compile
//#endif

#include <immintrin.h>


int main(int argc, char** argv)
{
    //__m256 a = _mm256_set_ps(8.0, 7.0, 6.0, 5.0, 4.0, 3.0, 2.0, 1.0);

    __m256i a = _mm256_set_epi32(8, 7, 6, 5, 4, 3, 2, 1);

    //__m256 b = _mm256_set_ps(18.0, 17.0, 16.0, 15.0, 14.0, 13.0, 12.0, 11.0);

    __m256i b = _mm256_set_epi32(18, 17, 16, 15, 14, 13, 12, 11);

    //__m256 c = _mm256_add_ps(a, b);

    __m256i c = _mm256_add_epi32(a, b);

    // should equal [12,14,16,18,20,22,24,26]

    int d[8];
    //_mm256_storeu_ps(d, c);

    _mm256_storeu_epi32(d, c);

    std::cout << "result equals " << d[0] << "," << d[1]
        << "," << d[2] << "," << d[3] << ","
        << d[4] << "," << d[5] << "," << d[6] << ","
        << d[7] << std::endl;

    return 0;
}