using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class ClockStaticAln
{
    private int m_tick0;
    private int m_tm0;
    public ClockStaticAln()
    {
    }
    public void StartClock()
    {
        DateTime now = DateTime.Now;
        int[] v = { now.Hour,   now.Minute, now.Second, now.Millisecond };
        int[] w = { 60,         60,         1000,       1};
        m_tm0 = 0;
        for (int i = 0; i < v.Length; i++)
        {
            m_tm0 += v[i];
            m_tm0 *= w[i];
        }

        m_tick0 = Environment.TickCount;
    }

    public int GetTickCnt()
    {
        int delta = Environment.TickCount - m_tick0;
        return delta + m_tm0;
    }

};

