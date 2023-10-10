#pragma once

typedef void (*VoidFunctionPtr)(void);

__declspec(dllexport) VoidFunctionPtr myFunction(int variable);
