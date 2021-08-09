using System.Threading.Tasks;
using UnityEngine;

public class TaskExample : MonoBehaviour
{
    void Start()
    {
        Task<int> t = new Task<int>(n => Sum((int)n), 10000);
        t.Start();
        t.Wait();
    }

    private static int Sum(int n)
    {
        int sum = 0;

        for(;n>0;n--)
            checked
            {
                sum += n;
            }

        return sum;
    }
}
