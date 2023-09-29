#include <vector>
#include "VHACD.h"

#ifdef WIN32
#define EXTERN  extern "C" __declspec(dllexport)
#else
#define EXTERN  extern "C"
#endif

struct ConvexHullCSharp {
    double* m_points;
    uint32_t* m_triangles;
    uint32_t m_nPoints;
    uint32_t m_nTriangles;
};

EXTERN void* CreateVHACD()
{
    return VHACD::CreateVHACD();
}

EXTERN void DestroyVHACD(void* pVHACD)
{
    auto vhacd = (VHACD::IVHACD*)pVHACD;
    vhacd->Clean();
    vhacd->Release();
}

EXTERN bool ComputeFloat(
    void* pVHACD,
    const float* const points,
    const uint32_t countPoints,
    const uint32_t* const triangles,
    const uint32_t countTriangles,
    const void*  params)
{
    auto vhacd = (VHACD::IVHACD*)pVHACD;
    return vhacd->Compute(points, countPoints, triangles, countTriangles, *(VHACD::IVHACD::Parameters const *)params);
}

EXTERN bool ComputeDouble(
    void* pVHACD,
    const double* const points,
    const uint32_t countPoints,
    const uint32_t* const triangles,
    const uint32_t countTriangles,
    const void* params)
{
    auto vhacd = (VHACD::IVHACD*)pVHACD;
    return vhacd->Compute(points, countPoints, triangles, countTriangles, *(VHACD::IVHACD::Parameters const *)params);
}

EXTERN uint32_t GetNConvexHulls(
    void* pVHACD
    )
{
    auto vhacd = (VHACD::IVHACD*)pVHACD;
    return vhacd->GetNConvexHulls();
}

EXTERN void GetConvexHull(
    void* pVHACD,
    const uint32_t index,
    void* convexHull)
{
    auto vhacd = (VHACD::IVHACD*)pVHACD;
    VHACD::IVHACD::ConvexHull ch;
    vhacd->GetConvexHull(index, ch);
    ConvexHullCSharp* convexHullCSharp = (ConvexHullCSharp*) convexHull;

    convexHullCSharp->m_nPoints = ch.m_points.size();
    convexHullCSharp->m_points = new double[convexHullCSharp->m_nPoints * 3];
    for(uint32_t i = 0; i < ch.m_points.size(); i++) {
        convexHullCSharp->m_points[i * 3 + 0] = ch.m_points[i].mX;
        convexHullCSharp->m_points[i * 3 + 1] = ch.m_points[i].mY;
        convexHullCSharp->m_points[i * 3 + 2] = ch.m_points[i].mZ;
    }

    convexHullCSharp->m_nTriangles = ch.m_triangles.size();
    convexHullCSharp->m_triangles = new uint32_t[convexHullCSharp->m_nTriangles * 3];
    for(uint32_t i = 0; i < ch.m_triangles.size(); i++) {
        convexHullCSharp->m_triangles[i * 3 + 0] = ch.m_triangles[i].mI0;
        convexHullCSharp->m_triangles[i * 3 + 1] = ch.m_triangles[i].mI1;
        convexHullCSharp->m_triangles[i * 3 + 2] = ch.m_triangles[i].mI2;
    }
}

EXTERN void FreeConvexHull(
    void* convexHull)
{
    delete [] ((ConvexHullCSharp*) convexHull)->m_points;
    delete [] ((ConvexHullCSharp*) convexHull)->m_triangles;
}