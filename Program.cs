using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.Sql;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.UI.HtmlControls;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace BlueDolphin.Renewal
{
    class BlueDolphinRenewal
    {

        public bool USE_PCONNECT = false; // use persistent connections?
        public string DEFAULT_PENDING_COMMENT = "Renewal Order has been created.";
        public string CHARSET = "iso-8859-1";
        public string FULFILLMENT_FULFILL_ID = "1";
        public string FULFILLMENT_CHANGE_ADDRESS_ID = "2";

        public string FULFILLMENT_CANCEL_ID = "3";
        public string FULFILLMENT_IGNORE_FULFILLMENT_ID = "4";
        public string PAPER_INVOICE_DATE_FORMAT = "Ymd_His";
        public string DEFAULT_COUNTRY_ID = "223";
        public string MODULE_PAYMENT_PAYFLOWPRO_TEXT_ERROR = "Credit Card Error!";
        public string DATE_FORMAT_DB = "%Y-%m-%d %H:%M:%S";
        public string FILENAME_PRODUCT_INFO = "product_info.php";

        //the following are defined in renewal_track_emails table.
        public string TRACK1 = "1014";
        public string TRACK2_BAD_CC = "1015";
        public string TRACK2_CHECK = "1016";
        public string TRACK2_MC = "1,.017";
        public string TRACK2_PC = "1018";

        public static int number_of_renewal_orders_created;
        public static int number_of_renewal_orders_charged;
        public static int number_of_renewal_invoices_created;
        public static int number_of_additional_renewal_invoices_created;
        public static int number_of_renewal_email_invoices_sent;

        public static int number_of_renewal_paper_invoices_file_records;
        public static int number_of_invoices_cleaned_up;
        public static int number_of_renewal_orders_mass_cancelled;

        DatabaseTables dt = new DatabaseTables();

        public static string connectionString = ConfigurationManager.ConnectionStrings["databaseConnectionString"].ToString();
        public static MySqlConnection myConn = new MySqlConnection(connectionString);

        static void Main(string[] args)
        {
            try
            {

                // Set our email body string for our e-mail to an empty string.
                string email_body = string.Empty;

                //this allows the script to run without any maximum executiont time.
                // set_time_limit(0)  Do we need this in .NET?;

                myConn.Open();

                //set up logging of script to file

                Console.WriteLine("Begin renewal main"+"\n");
                email_body += "Begin renewal main \n\n";

                Console.WriteLine("Begin init_renewal_orders");
                number_of_renewal_orders_created = init_renewal_orders();
                Console.WriteLine("End init_renewal_orders. number of renewal orders created: " + number_of_renewal_orders_created.ToString() + "\n");
                email_body += "End init_renewal_orders. number of renewal orders created: " + number_of_renewal_orders_created.ToString()+"\n\n";

                //let's charge first since if it fails we can create the 1015 right after this.
	            Console.WriteLine("Begin charging renewal orders");
	            number_of_renewal_orders_charged = charge_renewal_orders();
                Console.WriteLine("End charging renewal orders. number of renewal orders charged: " + number_of_renewal_orders_charged.ToString() + "\n");
	            email_body += "End charging renewal orders. number of renewal orders charged: " + number_of_renewal_orders_charged.ToString()+ "\n\n";
                
                Console.WriteLine("Begin creating renewal invoices");
	            number_of_renewal_invoices_created = create_first_effort_renewal_invoices();
                Console.WriteLine("End creating renewal invoices. number of renewal invoices created: " + number_of_renewal_invoices_created.ToString() + "\n");
	            email_body += "End creating renewal invoices. number of renewal invoices created: " + number_of_renewal_invoices_created.ToString() +"\n\n";

                Console.WriteLine("Begin creating additional renewal invoices");
                number_of_additional_renewal_invoices_created = create_additional_renewal_invoices();
                Console.WriteLine("End creating additional renewal invoices. number of additional renewal invoices created: " + number_of_additional_renewal_invoices_created.ToString() + "\n");
                email_body += "End creating additional renewal invoices. number of additional renewal invoices created: " + number_of_additional_renewal_invoices_created.ToString() + "\n\n";

                Console.WriteLine("Begin sending renewal email invoices");
                number_of_renewal_email_invoices_sent = send_renewal_email_invoices();
                Console.WriteLine("End sending renewal email invoices. number of renewal email invoices sent: " + number_of_renewal_email_invoices_sent.ToString() + "\n");
                email_body += "End sending renewal email invoices. number of renewal email invoices sent: " + number_of_renewal_email_invoices_sent.ToString() + "\n\n";

                Console.WriteLine("Begin creating renewal paper invoices file");
                number_of_renewal_paper_invoices_file_records = create_renewal_paper_invoices_file();
                Console.WriteLine("End creating renewal paper invoices file. number of renewal paper invoices file records: " + number_of_renewal_paper_invoices_file_records.ToString() + "\n");
                email_body += "End creating renewal paper invoices file. number of renewal paper invoices file records: " + number_of_renewal_paper_invoices_file_records.ToString() + "\n\n";
              
                Console.WriteLine("Begin cleaning up renewal invoices");
                number_of_invoices_cleaned_up = clean_up_renewal_invoices();
                Console.WriteLine("End cleaning up renewal invoices. number of renewal invoices cleaned up: " + number_of_invoices_cleaned_up.ToString() + "\n");
                email_body += "End cleaning up renewal invoices. number of renewal invoices cleaned up: " + number_of_invoices_cleaned_up.ToString() + "\n\n";

                //now see if we need to cancel any renewal orders.
                Console.WriteLine("Begin mass cancelling renewal orders");
                number_of_renewal_orders_mass_cancelled = mass_cancel_renewal_orders();
                Console.WriteLine("End mass cancelling renewal orders. number of renewal orders mass cancelled: " + number_of_renewal_orders_mass_cancelled.ToString() + "\n");
                email_body += "End mass cancelling renewal orders. number of renewal orders mass cancelled: " + number_of_renewal_orders_mass_cancelled.ToString() + "\n\n";

                Console.WriteLine("End renewal main");
	            email_body += "End renewal main \n\n";

                // Send e-mail saying we have completed the renewal run.
	            //tep_mail('M2 Media Group Jobs', 'jobs@m2mediagroup.com', 'Renewal Process Successful', $email_body, 'BlueDolphin', 'jobs@m2mediagroup.com', '', '',false);
	            //tep_mail('Michael Borchetta', 'mborchetta@m2mediagroup.com', 'Renewal Process Successful', $email_body, 'BlueDolphin', 'jobs@m2mediagroup.com', '', '',false);
	            //tep_mail('Martin Schmidt', 'mschmidt@mcswebsolutions.com', 'Renewal Process Successful', $email_body, 'BlueDolphin', 'jobs@m2mediagroup.com', '', '',false);
                
                myConn.Close();

                Console.ReadLine();
            }

            catch (Exception e)
            {
                
                Console.WriteLine(e.Message);

            }

        }

       
        private static int init_renewal_orders()
        {
            try
            {
                //there was a problem with products_quantity being in both orders_products and products,
                //with 2 different meanings. So we just pick what we need from product and get the rest

                //select all orders that have a continuous_service, with no renewal invoices created,
                // user want to renew (auto_renew), paid orders and renewal notice < today.

                string original_order_products_id = string.Empty;
                string original_order_skus_type_order = string.Empty;

                string create_renewal_orders_query_string = @"
		SELECT
			o.`renewal_payment_cards_id`,
			pc.cc_type AS renewal_cc_type,
			pc.cc_number AS renewal_cc_number,
			pc.cc_number_display AS renewal_cc_number_display,
			pc.cc_expires AS renewal_cc_expires,
			pc.cc_owner AS renewal_cc_owner,
			o.*, op.*, s.*,
			p.continuous_service,
			p.products_status
		FROM
			orders o LEFT JOIN payment_cards pc ON (pc.payment_cards_id = o.renewal_payment_cards_id),
			orders_products op,
			products p,
			skus s
		WHERE
			o.orders_id = op.orders_id
			AND op.products_id = p.products_id
			AND op.skus_id = s.skus_id
			AND p.continuous_service = 1
			AND o.auto_renew = 1
			AND o.renewal_error != 1
			AND o.orders_status = 2
			AND o.renewal_date is not null
			AND to_days(o.renewal_date) > to_days(DATE_SUB(curdate(),INTERVAL 60 DAY))
			AND to_days(o.renewal_date) <= to_days(curdate())
	";

                MySqlCommand command = new MySqlCommand(string.Empty, myConn);
                command.CommandText = create_renewal_orders_query_string;
                command.ExecuteNonQuery();

                MySqlDataReader reader = command.ExecuteReader();


                string potential_renewal_skus_query_string = @"
			select
				*,
				if(p.first_issue_delay_days=0,pf.first_issue_delay_days, p.first_issue_delay_days) as first_issue_delay_days
			from
				skus s,
				products p,
				publication_frequency pf
			where
				s.products_id = " + original_order_products_id + @"
				and s.skus_type = 'RENEW'
				and s.skus_type_order = " + original_order_skus_type_order + @"
				and s.skus_status = 1
				and s.fulfillment_flag = 1
				and s.products_id = p.products_id
				and p.publication_frequency_id = pf.publication_frequency_id
			order by
				s.skus_type_order_period desc
		";

                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;

            }

        }

        private static int charge_renewal_orders()
        {
            try
            {
                
                //make sure we have sent them renewalinvoices
	//eg IF orderItem.renewal_invoices_sent > 0 THEN
	//
	//if the charge fails we need to put the user in track 2 1015
	//move all invoices from renewals_invoices to renewal_invoices_history with
	//comments: Changed from 1014 to 1015.
	//create new invoices based for 1015.
	//make sure to update the order's renewals_billing_series_id to the new one.

	//make sure the order is still PENDING, they might have paid already.

	string charge_renewal_orders_query_string = @"
		select
			sk.override_renewal_billing_descriptor,
			o.*, op.*, s.*,
			p.continuous_service,
			p.products_status,
			pd.products_billing_descriptor
		from
			orders o,
			orders_products op,
			products p,
			products_description pd,
			skus s,
			skinsites sk
		where
			o.orders_id = op.orders_id
			and sk.skinsites_id = o.skinsites_id
			and op.products_id = p.products_id
			and op.skus_id = s.skus_id
			and o.is_renewal_order = 1
			and o.renewal_transaction_date is not null
			and o.renewal_error != 1
			and to_days(o.renewal_transaction_date) > to_days(DATE_SUB(curdate(),INTERVAL 30 DAY))
			and to_days(o.renewal_transaction_date) <= to_days(curdate())
			and pd.products_id = op.products_id
	";

                var transaction = new Dictionary<string, string>();

                transaction["USER"] = "";
                transaction["VENDOR"] = "";
                transaction["PARTNER"] = "";
                transaction["PWD"] = "";
                transaction["TRXTYPE"] = "";
                transaction["USER"] = "";
                transaction["VENDOR"] = "";
                transaction["PARTNER"] = "";
                transaction["PWD"] = "";
                transaction["TRXTYPE"] = "";
                transaction["USER"] = "";
                transaction["VENDOR"] = "";
                transaction["PARTNER"] = "";
                transaction["PWD"] = "";
                transaction["TRXTYPE"] = "";
                transaction["USER"] = "";
                transaction["VENDOR"] = "";
                transaction["PARTNER"] = "";
                transaction["PWD"] = "";
                transaction["TRXTYPE"] = "";
                transaction["USER"] = "";
                transaction["VENDOR"] = "";
                transaction["PARTNER"] = "";
                transaction["PWD"] = "";
                transaction["TRXTYPE"] = "";
                transaction["USER"] = "";
                transaction["VENDOR"] = "";
                transaction["PARTNER"] = "";
                transaction["PWD"] = "";
                transaction["TRXTYPE"] = "";

                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;

            }
        }

        private static int create_first_effort_renewal_invoices()
        {
            try
            {

                string renewals_billing_series_id = string.Empty;
                string renewals_billing_series_delay = String.Empty;
                

                //only grab the last 30 days worth. No need to get all orders ever.
		string renewal_orders_query_string = @"
			select
				o.*, op.*, s.*, p.continuous_service, p.products_status
			from
				orders o,
				orders_products op,
				products p,
				skus s
			where
				o.orders_id = op.orders_id
				and op.products_id = p.products_id
				and op.skus_id = s.skus_id
				and o.renewal_invoices_created = 0
				and o.renewal_invoices_sent = 0
				and o.orders_status = 1
				and o.is_renewal_order = 1
				and o.renewals_billing_series_id = " + renewals_billing_series_id + @"
				and to_days(o.date_purchased) > to_days(DATE_SUB(curdate(),INTERVAL 60 DAY))
				and to_days(o.date_purchased) <= to_days(DATE_SUB(curdate(),INTERVAL " + renewals_billing_series_delay + @" DAY))
		";

                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;

            }

        }

        private static int create_additional_renewal_invoices()
        {
            //go through only pending orders, which haven't been sent yet and are in progress

            try
            {
                int number_of_renewal_invoices_created = 0;

                //rearrange the billing series array so we can pick the next effort
	            //for($i=0, $n=sizeof($renewals_billing_series_array); $i<$n;$i++) {
		        //$renewels_billing_series[$renewals_billing_series_array[$i]['renewals_billing_series_id']][$renewals_billing_series_array[$i]['effort_number']] = $renewals_billing_series_array[$i];
	
                //rearrange the billing series array so we can pick the next effort
	                //for($i=0, $n=sizeof($renewals_billing_series_array); $i<$n;$i++) {
		                //$renewels_billing_series[$renewals_billing_series_array[$i]['renewals_billing_series_id']][$renewals_billing_series_array[$i]['effort_number']] = $renewals_billing_series_array[$i];
                //	}

                            MySqlCommand command = new MySqlCommand(string.Empty, myConn);
                            command.CommandText = @"
		            select *
		            from renewals_invoices ri,
			            orders o,
			            orders_products op,
			            renewals_billing_series rbs,
			            skus s,
			            products p
		            where ri.orders_id=o.orders_id
			            and o.orders_id = op.orders_id
			            and op.skus_id = s.skus_id
			            and op.products_id = p.products_id
			            and o.renewals_billing_series_id = rbs.renewals_billing_series_id
			            and rbs.renewals_billing_series_id = ri.renewals_billing_series_id
			            and rbs.effort_number = ri.effort_number
			            and ri.was_sent=1
			            and ri.in_progress=1
	            ";
                command.ExecuteNonQuery();

                MySqlDataReader myReader;
                myReader = command.ExecuteReader();
                while (myReader.Read())
                {
                   var renewals_invoices_id = myReader["renewals_invoices_id"];
		           var customers_id = myReader["customers_id"];
		           var orders_id = myReader["orders_id"];
		           var renewals_billing_series_id = myReader["renewals_billing_series_id"];
		           int renewals_billing_series_effort_number = Convert.ToInt32(myReader["effort_number"]);
		           var  products_id = myReader["products_id"];
		           var skus_type_order =myReader["skus_type_order"];
		           var prior_orders_id = myReader["prior_orders_id"];
		           var renewal_order_status = myReader["orders_status"];
		           var skus_status = myReader["skus_status"];
		           var date_sent = myReader["date_sent"];
		           var continuous_service = myReader["continuous_service"];
		           var auto_renew = myReader["auto_renew"];

		           bool create_next_effort = true;

		           int next_effort_number = renewals_billing_series_effort_number+1;

                }

                myReader.Close();

                //check to see if the order is still valid for invoice creation, if not then update the invoice
		//and move on to next order.
	/*	$check_renewal_order_result = check_renewal_order($skus_type_order, $skus_status, $products_id, $prior_orders_id, $continuous_service, $auto_renew, $renewal_order_status);
		if ($check_renewal_order_result !== true) {
			$comment = "Next effort for this invoice was not created because " . $check_renewal_order_result;
			tep_db_query("update renewals_invoices set in_progress = 0, comments='" . tep_db_input($comment) . "' where renewals_invoices_id = '" . $renewals_invoices_id . "'");
			continue;
		}

		//check to see if there is a next effort for this series.
		if (!isset($renewels_billing_series[$renewals_billing_series_id][$next_effort_number])) {
			$create_next_effort = false;
			$comment = "Next effort for this invoice was not created because there are no more efforts for this billing series.";
		} else {
			$next_effort_delay = $renewels_billing_series[$renewals_billing_series_id][$next_effort_number]['delay_in_days'];
			$comment = "Next effort for this invoice was created.";
		}

		if ($create_next_effort) {
			//this is where we create the next one.
			//Let's check to make sure the user hasn't already been entered for the same order
			//if so the unique index will be violated and an error returned. Using the tep_db_query_return_error version of the
			// it will allow us to continue. Which is what we want here. We add the delay here.
			$create_renewal_invoice_query_string = "insert into renewals_invoices (date_to_be_sent, orders_id, customers_id, renewals_billing_series_id, effort_number, in_progress)
                      values (DATE_ADD(curdate(),INTERVAL " . $next_effort_delay . " DAY), '" . $orders_id . "', '" . $customers_id . "', '" . $renewals_billing_series_id . "', $next_effort_number, '1')";

			$result = tep_db_query_return_error($create_renewal_invoice_query_string);

			//if there was an error let's record that.
			if (tep_db_query_returned_error()) {
				log_renewal_process("Warning: create_additional_renewal_invoice tried to insert the same user,same order, same effort (" . $create_renewal_invoice_query_string . ")", $orders_id);
			}
			$number_of_renewal_invoices_created++;

		}

		//set this invoice' in_progress to 0. Used for clean up later.
		if ($create_next_effort) {
			tep_db_query("update renewals_invoices set in_progress = 0, comments='" . tep_db_input($comment) . "' where renewals_invoices_id = '" . $renewals_invoices_id . "'");
		} else {
			//don't set the in_progress to 0 since it is the last effort. We'll clean this one up during
			//mass cancel,since it is allowed to be active for cancel delay days.
			tep_db_query("update renewals_invoices set comments='" . tep_db_input($comment) . "' where renewals_invoices_id = '" . $renewals_invoices_id . "'");

		}
	}*/


                return number_of_renewal_invoices_created;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;

            }
           
        }

        private static int send_renewal_email_invoices()
        {
            try
            {


                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;

            }
        }

        private static int create_renewal_paper_invoices_file()
        {
            try
            {
              
                // Go through only pending orders, which haven't been sent yet and are in progress
	            string renewal_invoices_info_query_string = @"select *
												from renewals_invoices ri,
													 orders o,
													 orders_products op,
												     renewals_billing_series rbs,
 													 skus s,
 													 products p,
												     skinsites ss
												where ss.skinsites_id = o.skinsites_id
											  	and ri.orders_id=o.orders_id
												and o.orders_id = op.orders_id
												and op.skus_id = s.skus_id
												and op.products_id = p.products_id
												and o.renewals_billing_series_id = rbs.renewals_billing_series_id
 												and rbs.renewals_billing_series_id = ri.renewals_billing_series_id
        										and rbs.effort_number = ri.effort_number
												and ri.was_sent=0
                  								and ri.in_progress=1
												and to_days(ri.date_to_be_sent) <= to_days(curdate())
 												and rbs.renewals_invoices_type = 'PAPER'";

                // Set our number of processed paper invoices to its default value of zero.
	            int number_of_renewal_paper_invoices_file_records = 0;

                MySqlCommand command = new MySqlCommand(string.Empty, myConn);
                command.CommandText = renewal_invoices_info_query_string;
                command.ExecuteNonQuery();

                MySqlDataReader myReader;
                myReader = command.ExecuteReader();
                while (myReader.Read())
                {
                   // Pull data form our current renewal invoice.
	/*	$billing_first_name = $renewal_invoices_info['billing_first_name'];
		$billing_last_name = $renewal_invoices_info['billing_last_name'];
		$billing_address_line_1 = $renewal_invoices_info['billing_street_address'];
		$billing_city = $renewal_invoices_info['billing_city'];
		$billing_state = $renewal_invoices_info['billing_state'];
		$billing_postal_code = $renewal_invoices_info['billing_postcode'];
		$delivery_first_name = $renewal_invoices_info['delivery_first_name'];
		$delivery_last_name = $renewal_invoices_info['delivery_last_name'];
		$delivery_address_line_1 = $renewal_invoices_info['delivery_street_address'];
		$delivery_city = $renewal_invoices_info['delivery_city'];
		$delivery_state = $renewal_invoices_info['delivery_state'];
		$delivery_postal_code = $renewal_invoices_info['delivery_postcode'];
		$renewals_invoices_id = $renewal_invoices_info['renewals_invoices_id'];
		$customers_id = $renewal_invoices_info['customers_id'];
		$orders_id = $renewal_invoices_info['orders_id'];
		$renewals_billing_series_code = $renewal_invoices_info['renewals_billing_series_code'];
		$products_id = $renewal_invoices_info['products_id'];
		$skus_type_order =$renewal_invoices_info['skus_type_order'];
		$prior_orders_id = $renewal_invoices_info['prior_orders_id'];
		$renewal_order_status = $renewal_invoices_info['orders_status'];
		$skus_status = $renewal_invoices_info['skus_status'];
		$products_name = $renewal_invoices_info['products_name'];
		$skus_term = $renewal_invoices_info['skus_term'];
		$effort_number = $renewal_invoices_info['effort_number'];
		$date_purchased = $renewal_invoices_info['date_purchased'];
		$amount_owed = $renewal_invoices_info['amount_owed'];
		$amount_paid = $renewal_invoices_info['amount_paid'];
		$price = $renewal_invoices_info['products_price'];
		$email_address = $renewal_invoices_info['customers_email_address'];
		$continuous_service = $renewal_invoices_info['continuous_service'];
		$auto_renew = $renewal_invoices_info['auto_renew'];
		$cc_number_display = $renewal_invoices_info['cc_number_display'];
		$template_directory = $renewal_invoices_info['tplDir'];
		$skinsites_id = $renewal_invoices_info['skinsites_id'];

		// Check to make sure we can still process this paper invoice.
		// If not print why and stop processing renewal invoice.
		$check_renewal_order_result = check_renewal_order($skus_type_order, $skus_status, $products_id, $prior_orders_id, $continuous_service, $auto_renew, $renewal_order_status);
		if ($check_renewal_order_result !== true) {
			//set the in_progress to 0. Used for clean up later.
			$comments = "This paper effort was not created because " . $check_renewal_order_result;
			tep_db_query("update renewals_invoices set in_progress = 0, comments = '" . $comments . "' where renewals_invoices_id = '" . $renewals_invoices_id . "'");
			continue;
		}

		// Insert a new row into our paper invoices file
		tep_db_query("insert into paper_invoices (customers_id, billing_first_name, billing_last_name, billing_address_line_1, billing_address_line_2, billing_city, billing_state,
					billing_postal_code, delivery_first_name, delivery_last_name, delivery_address_line_1, delivery_address_line_2, delivery_city, delivery_state,
					delivery_postal_code, product_name, price, term, effort_number, orders_id, date_purchased, amount_owed, amount_paid, email_address,
					renewals_billing_series_code, cc_number_display, template_directory, site_id, created_date, modified_date, active)
					values ('" . $customers_id . "', '" . tep_db_input($billing_first_name) . "', '" . tep_db_input($billing_last_name) . "', '" . tep_db_input($billing_address_line_1) . "', '', '" . tep_db_input($billing_city) . "', '" . $billing_state . "',
					'" . tep_db_input($billing_postal_code) . "', '" . tep_db_input($delivery_first_name) . "', '" . tep_db_input($delivery_last_name) . "', '" . tep_db_input($delivery_address_line_1) . "', '', '" . tep_db_input($delivery_city) . "', '" . $delivery_state . "',
					'" . tep_db_input($delivery_postal_code) . "', '" . tep_db_input($products_name) . "', '" . $price . "', '" . $skus_term . "', '" . $effort_number . "', '" . $orders_id . "', '" . $date_purchased . "',
					'" . $amount_owed . "', '" . $amount_paid . "', '" . $email_address . "', '" . tep_db_input($renewals_billing_series_code) . "', '" . tep_db_input($cc_number_display) . "', '" . tep_db_input($template_directory) . "', '" . $skinsites_id . "', now(), now(), 1)");

		// Increment our number of papaer invoices by one.
		$number_of_renewal_paper_invoices_file_records++;

		// Update the was_sent flag.
		tep_db_query("update renewals_invoices
					  set was_sent=1, date_sent=now()
					  where renewals_invoices_id='" . $renewals_invoices_id . "'");

		// Update the order's invoices_sent flag.
		tep_db_query("update orders set renewal_invoices_sent=1 where orders_id='" . $orders_id . "'");
     * 
     * */
                }

                myReader.Close();

                return number_of_renewal_paper_invoices_file_records;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;

            }
        }

        private static int clean_up_renewal_invoices()
        {

            //move any renewal invoice where in progress is 0 or was sent.

            try
            {

                int number_of_renewal_invoices_cleaned_up = 0;
                int renewals_invoices_id;

                //LOOP THROUHG ALL INVOICES WHERE IN_PROGRESS IS 0.
                MySqlCommand command = new MySqlCommand(string.Empty,myConn);
                command.CommandText = "select * from renewals_invoices ri where ri.in_progress=0";
                command.ExecuteNonQuery();
                
                MySqlDataReader myReader;
                myReader = command.ExecuteReader();
                while (myReader.Read())
                {
                    Console.WriteLine(myReader["renewals_invoices_id"]);
                    renewals_invoices_id = Convert.ToInt32(myReader["renewals_invoices_id"]);

                     //move the invoice to history.
		             //we use replace. If the server goes down right between these 2 stmts then the next time
		             //it will still work.
		             MySqlCommand command2 = new MySqlCommand("replace into renewals_invoices_history select * from renewals_invoices where renewals_invoices_id = " + renewals_invoices_id.ToString(), myConn);
                     command2.ExecuteNonQuery();
                     //remove old one
                     MySqlCommand command3 = new MySqlCommand("delete from renewals_invoices where renewals_invoices_id = " + renewals_invoices_id.ToString(), myConn);
                     command3.ExecuteNonQuery();
                    
                    number_of_renewal_invoices_cleaned_up++;
                }

                myReader.Close();

                return number_of_renewal_invoices_cleaned_up;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;

            }
        }

        private static int mass_cancel_renewal_orders()
        {

            //mass cancel renewal orders. We look at the cancel_delay on the billing series
            //which we will add to the time the last invoice was sent and if the time
            //has expired we simply cancel the order (if it was still Pending).
            //we also need to cancel any still Pending orders that have moved to history.

            try
            {
                int number_of_mass_cancelled_orders = 0;

                MySqlCommand command = new MySqlCommand(string.Empty, myConn);
                command.CommandText = @"select ri.*, o.*, rbs.*
												from renewals_invoices ri,
													 orders o,
												     renewals_billing_series rbs
												where ri.orders_id=o.orders_id
												and o.renewals_billing_series_id = rbs.renewals_billing_series_id
 												and rbs.renewals_billing_series_id = ri.renewals_billing_series_id
        										and rbs.effort_number = ri.effort_number
												and o.orders_status = 1
												and ri.in_progress = 1
												and rbs.cancel_delay is not null
												and to_days(now()) > to_days(DATE_ADD(ri.date_sent,INTERVAL rbs.cancel_delay DAY))";
                command.ExecuteNonQuery();

                return number_of_mass_cancelled_orders;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;

            }
        }

        private static DateTime get_renewal_date()
        {
            try
            {

                return DateTime.Now;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return DateTime.Now;
                

            }
        }

        private static bool check_renewal_order()
        {

            //If a renewal order is placed, at the time of the sending of email or charging the card,
            //or getting check, the product and sku could be changed to
            //inactive. If the product is inactive and there are no renewal sku active at all for that
            //product then don't send email and don't renew.
            // if the product is inactive and the skus_id (on orders_products.skus_id on the renewal order
            // pulled here) is inactive (Matt has changed the price/remit)
            // then we need to do a quick check to see if there is at least another active for that
            // product for the same skus_type_order. Fulfillment will take care of the rest.

            try
            {

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;


            }
        }

    }
}
