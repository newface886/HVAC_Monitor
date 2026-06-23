# Project Environment Configuration

## Deep Learning Environment (Conda)
- **环境名称**: `pytorch_env`
- **Python 路径**: `C:\Users\fang rui\.conda\envs\pytorch_env\python.exe`
- **Python 版本**: 3.10.20 (Anaconda)
- **PyTorch Version**: 2.6.0+cu124
- **CUDA Version**: 12.4
- **GPU**: NVIDIA GeForce RTX 4060 Laptop GPU (8GB VRAM)
- **Conda 版本**: 25.5.1

## Important Paths
- When running Python scripts, always use: `C:\Users\fang rui\.conda\envs\pytorch_env\python.exe`
- When running pip commands, use: `C:\Users\fang rui\.conda\envs\pytorch_env\python.exe -m pip`
- When running pytest, use: `C:\Users\fang rui\.conda\envs\pytorch_env\python.exe -m pytest`

## Usage Notes
- PyTorch is installed with CUDA 12.4 support
- Use `torch.cuda.is_available()` to verify GPU access
- Default device should be set to `cuda` when GPU is available
