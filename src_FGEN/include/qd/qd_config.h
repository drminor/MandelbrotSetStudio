/* include/qd/qd_config.h.  Generated from qd_config.h.in by configure.  */
#ifndef _QD_QD_CONFIG_H
#define _QD_QD_CONFIG_H  1

#ifndef QD_API
#define QD_API /**/
#endif

/* If fused multiply-add is available, define to correct macro for
   using it.  It is invoked as QD_FMA(a, b, c) to compute fl(a * b + c). 
   If correctly rounded multiply-add is not available (or if unsure), 
   keep it undefined.*/
#ifndef QD_FMA
/* #undef QD_FMA */
#endif

/* If fused multiply-subtract is available, define to correct macro for
   using it.  It is invoked as QD_FMS(a, b, c) to compute fl(a * b - c). 
   If correctly rounded multiply-add is not available (or if unsure), 
   keep it undefined.*/
#ifndef QD_FMS
/* #undef QD_FMS */
#endif

/* Define this macro to be the isfinite(x) function. */
#define QD_ISFINITE(x) ( ::_finite(x) != 0 )

/* Define this macro to be the isinf(x) function. */
#define QD_ISINF(x) ( ( ::_fpclass(x) & (_FPCLASS_NINF | _FPCLASS_PINF ) ) != 0 )

/* Define this macro to be the isnan(x) function. */
#define QD_ISNAN(x) ( ::_isnan(x) != 0 )


#endif /* _QD_QD_CONFIG_H */
