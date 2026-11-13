namespace BlazorXe_Xemay.Features.DonHang;

public sealed class SoSanhXeService
{
    private readonly HashSet<int> _productIds = [];

    public event Action? Changed;

    public IReadOnlyCollection<int> ProductIds => _productIds;

    public void Them(int xeId)
    {
        if (xeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(xeId));
        }

        if (_productIds.Count >= 4 && !_productIds.Contains(xeId))
        {
            throw new InvalidOperationException("Mỗi lần so sánh tối đa 4 mẫu xe.");
        }

        _productIds.Add(xeId);
        Changed?.Invoke();
    }

    public void Xoa(int xeId)
    {
        _productIds.Remove(xeId);
        Changed?.Invoke();
    }
}

public sealed class GioHangService
{
    private readonly Dictionary<int, int> _items = [];

    public event Action? Changed;

    public IReadOnlyDictionary<int, int> Items => _items;

    public int TongSoLuong => _items.Values.Sum();

    public void Them(int xeId, int soLuong = 1)
    {
        if (xeId <= 0 || soLuong <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(soLuong), "Xe và số lượng phải hợp lệ.");
        }

        _items[xeId] = _items.GetValueOrDefault(xeId) + soLuong;
        Changed?.Invoke();
    }

    public void CapNhatSoLuong(int xeId, int soLuong)
    {
        if (soLuong < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(soLuong), "Số lượng không được âm.");
        }

        if (soLuong == 0)
        {
            _items.Remove(xeId);
        }
        else
        {
            _items[xeId] = soLuong;
        }

        Changed?.Invoke();
    }

    public void Xoa(int xeId)
    {
        _items.Remove(xeId);
        Changed?.Invoke();
    }

    public void XoaTatCa()
    {
        _items.Clear();
        Changed?.Invoke();
    }
}
