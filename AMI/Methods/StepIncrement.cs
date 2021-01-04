namespace Neitsillia.Methods
{
    static class StepIncrement
    {

        public static int SI(int step, int increment, int target, int start = 0)
        {
            int num = 0;
            for (int i = start; i < target; i += step)
                num += increment;
            return num;
        }
    }
}
