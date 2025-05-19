public interface IFlashable
{
    /// <summary>
    /// 사진 촬영 시 호출됩니다.
    /// </summary>
    /// <param name="isEnhanced">강화 촬영 여부(지금은 항상 false)</param>
    void OnPhotoTaken(bool isEnhanced);
}
