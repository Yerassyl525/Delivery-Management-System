#ifndef DELIVERY_NATIVE_H
#define DELIVERY_NATIVE_H

#if defined(_WIN32)
#define DELIVERY_API __declspec(dllexport)
#else
#define DELIVERY_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

DELIVERY_API int calculate_delivery_score(double distance_km, int is_express);

#ifdef __cplusplus
}
#endif

#endif
