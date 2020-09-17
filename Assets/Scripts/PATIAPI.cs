using SingletonPattern;

public class PATIAPI : Singleton<PATIAPI>
{
    public bool pati, line, xiaomi;

    void Start()
    {
        // TODO : 임시로 line 버전으로
        this.line = true;
    }
}