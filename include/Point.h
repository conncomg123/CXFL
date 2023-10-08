#ifndef POINT_H
#define POINT_H

#include "pugixml.hpp"

class Point {
private:
	pugi::xml_node root;
	double x, y;
public:
	Point(pugi::xml_node& pointNode);
	Point(const pugi::xml_node& pointNode);
	~Point();
	Point(const Point& point);
	double getX() const;
	void setX(double x);
	double getY() const;
	void setY(double y);
	pugi::xml_node& getRoot();
};

#endif // POINT_H