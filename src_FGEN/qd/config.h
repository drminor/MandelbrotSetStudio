/* config.h.  Generated from config.h.in by configure.  */
/* config.h.in.  Generated from configure.ac by autoheader.  */

/* Define to a macro mangling the given C identifier (in lower and upper
   case), which must not contain underscores, for linking with Fortran. */
/* #undef FC_FUNC */

/* Define to 1 if you have the <fpu_control.h> header file. */
#define HAVE_FPU_CONTROL_H 1

/* qd major version number */
#define MAJOR_VERSION 2

/* qd minor version number */
#define MINOR_VERSION 3

/* Name of package */
#define PACKAGE "qd"

/* Define to the address where bug reports for this package should be sent. */
#define PACKAGE_BUGREPORT "yozo@cs.berkeley.edu"

/* Define to the full name of this package. */
#define PACKAGE_NAME "qd"

/* Define to the full name and version of this package. */
#define PACKAGE_STRING "qd 2.3.15"

/* Define to the one symbol short name of this package. */
#define PACKAGE_TARNAME "qd"

/* Define to the home page for this package. */
#define PACKAGE_URL ""

/* Define to the version of this package. */
#define PACKAGE_VERSION "2.3.15"

/* qd patch number (sub minor version) */
#define PATCH_VERSION 15

/* Any special symbols needed for exporting APIs. */
#if defined( QD_EXPORTS )
#define QD_API __declspec(dllexport)
#else
#define QD_API __declspec(dllimport)
#endif

/* Define this macro to be the copysign(x, y) function. */
#define QD_COPYSIGN(x, y) ::_copysign(x, y)

/* If fused multiply-add is available, define correct macro for using it. */
/* #undef QD_FMA */

/* If fused multiply-subtract is available, define correct macro for using it.
   */
/* #undef QD_FMS */

/* Define this macro to be the isfinite(x) function. */
#define QD_ISFINITE(x) ( ::_finite(x) != 0 )

/* Define this macro to be the isinf(x) function. */
#define QD_ISINF(x) ( ( ::_fpclass(x) & (_FPCLASS_NINF | _FPCLASS_PINF ) ) != 0 )

/* Define this macro to be the isnan(x) function. */
#define QD_ISNAN(x) ( ::_isnan(x) != 0 )

/* Version number of package */
#define VERSION "2.3.15"

/* Whether to use x86 fpu fix. */
#define X86 1
