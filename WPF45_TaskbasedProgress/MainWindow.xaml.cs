using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace WPF45_TaskbasedProgress
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        ObservableCollection<Employee> Employees;

        CancellationTokenSource cancelToken;

        Progress<double> progressOperation;

        public MainWindow()
        {
            InitializeComponent();

            Employees = new ObservableCollection<Employee>();
            btnCancel.IsEnabled = false;
        }

        // Displaying Employees in DataGrid 
        private async void btnLoadEmployee_Click(object sender, RoutedEventArgs e)
        {
            cancelToken = new CancellationTokenSource();
            btnLoadEmployee.IsEnabled = false;
            btnCancel.IsEnabled = true;
            txtstatus.Text = "Loading.....";
            progressOperation = new Progress<double>(value => progress.Value = value);

            try
            {
            //    dgEmp.ItemsSource = await LoadEmployeesAsync(cancelToken.Token,progressOperation); ;
                var Emps = await LoadEmployeesAsync(cancelToken.Token, progressOperation);

                foreach (var item in Emps)
                {
                    dgEmp.Items.Add(item);
                }

                txtstatus.Text ="Operation Completed";
            }
            catch (OperationCanceledException ex)
            {
                txtstatus.Text ="Operation cancelled" + ex.Message;
            }
            catch (Exception ex)
            {
                txtstatus.Text = "Operation cancelled" + ex.Message;
            }
            finally
            {
                cancelToken.Dispose();
                btnLoadEmployee.IsEnabled = true;
                btnCancel.IsEnabled = false;
            }

            
        }

        
        // Async Method to Load Employees
        
        async Task<ObservableCollection<Employee>> LoadEmployeesAsync(CancellationToken ct, IProgress<double> progress)
        {
            Employees.Clear();
            var task = Task.Run(() => {
                var xDoc = XDocument.Load("Company.xml");

                var Res = (from emp in xDoc.Descendants("Employee")
                          select new Employee()
                          {
                               EmpNo = Convert.ToInt32(emp.Descendants("EmpNo").First().Value),
                               EmpName = emp.Descendants("EmpName").First().Value.ToString(),
                               Salary = Convert.ToInt32(emp.Descendants("Salary").First().Value)
                          }).ToList();
               

               int recCount = 0;

               foreach (var item in Res)
                {
                    Thread.Sleep(100);
                    ct.ThrowIfCancellationRequested();
                    Employees.Add(item);
                    ++recCount;
                    progress.Report(recCount * 100.0 / 50);
                }
                
                return Employees;
            
            });

            return await task;
        }

        private async void btnCalculateTax_Click(object sender, RoutedEventArgs e)
        {
            cancelToken = new CancellationTokenSource();
            btnLoadEmployee.IsEnabled = false;
            btnCalculateTax.IsEnabled = false;
            btnCancel.IsEnabled = true;
            txtstatus.Text = "Calculating.....";
            progressOperation = new Progress<double>(value => progress.Value = value);
            try
            {
                dgEmp.Items.Clear();
                int recCount = 0;

                foreach (Employee Emp in Employees)
                {
                    dgEmp.Items.Add(await CalculateTaxPerRecord(Emp, cancelToken.Token, progressOperation));
                    ++recCount;
                    ((IProgress<double>)progressOperation).Report(recCount * 100.0 / 50);
                }

                txtstatus.Text = "Operation Completed";
            }
            catch (OperationCanceledException ex)
            {
                txtstatus.Text = "Operation cancelled" + ex.Message;
            }
            catch (Exception ex)
            {
                txtstatus.Text = "Operation cancelled" + ex.Message;
            }
            finally
            {
                cancelToken.Dispose();
                btnCalculateTax.IsEnabled = true;
                btnCancel.IsEnabled = false;
            }
        }


        // Asynchromous Method to Calculate The Tax
        async Task<ObservableCollection<Employee>> CalculateTax(CancellationToken ct, IProgress<double> progress) 
        {
            ObservableCollection<Employee> EmployeesTax = new ObservableCollection<Employee>();
            EmployeesTax.Clear();
            var task = Task.Run(() =>
            {
                int recCount = 0;
                foreach (Employee Emp in Employees)
                {
                    Thread.Sleep(100);
                    ct.ThrowIfCancellationRequested();
                    Emp.Tax = Convert.ToInt32(Emp.Salary * 0.2);
                    EmployeesTax.Add(Emp);
                    ++recCount;
                    progress.Report(recCount * 100.0 / 50);
                }
                return EmployeesTax;
            });

            return await task;
        }

        // Calcluate the Tax for every Employee Record.
        async Task<Employee> CalculateTaxPerRecord(Employee Emp, CancellationToken ct, IProgress<double> progress)
        {
             var tsk = Task<Employee>.Run(() =>
             {
                    Thread.Sleep(100);
                    ct.ThrowIfCancellationRequested();
                    Emp.Tax = Convert.ToInt32(Emp.Salary * 0.2);
                    return Emp;
                });

            
            return await tsk;
        }

        
        
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            cancelToken.Cancel();
        }
    }

    public class Employee
    {
        public int EmpNo { get; set; }
        public string EmpName { get; set; }
        public int Salary { get; set; }
        public decimal Tax { get; set; }
    }
}
