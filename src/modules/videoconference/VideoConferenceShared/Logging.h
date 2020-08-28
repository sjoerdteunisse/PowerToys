#pragma once

#include <string>
#include <guiddef.h>
#include <system_error>

void LogToFile(std::string what);
std::string toMediaTypeString(GUID subtype);

#define RETURN_IF_FAILED_WITH_LOGGING(val)                                                             \
    hr = (val);                                                                                        \
    if (FAILED(hr))                                                                                    \
    {                                                                                                  \
        LogToFile(std::string(__FUNCTION__ "() ") + #val + ": " + std::system_category().message(hr)); \
        return hr;                                                                                     \
    }

#define RETURN_NULLPTR_IF_FAILED_WITH_LOGGING(val)                                                     \
    hr = val;                                                                                          \
    if (FAILED(hr))                                                                                    \
    {                                                                                                  \
        LogToFile(std::string(__FUNCTION__ "() ") + #val + ": " + std::system_category().message(hr)); \
        return nullptr;                                                                                \
    }
